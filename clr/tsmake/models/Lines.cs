using System;
using System.Runtime.CompilerServices;

namespace tsmake.models
{
    public class Location
    {
        public Stack<string> SourceFiles { get; }
        public string CurrentFileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        //public Location OriginalLocation { get; }

        public Location(Stack<string> sourceFiles, int lineNumber, int columnNumber)
        {
            this.SourceFiles = sourceFiles;

            this.CurrentFileName = sourceFiles.Peek();
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
        }

        public Location(Location parent, string fileName, int lineNumber, int columnNumber)
        {
            // TODO: need to figure out when/where to use this .ctor... 
        }

        public string GetLocationContext()
        {
            //return $"Location: [{this.FileName}]({this.LineNumber}, {this.ColumnNumber})";
            return "TODO: need to chain/concat full-source-file lineage thingies in Location.GetLocationContext().";
        }
    }

    public class Line
    {
        public Stack<string> SourceLocation { get; }
        public int LineNumber { get; }
        public string RawContent { get; }
        public string CodeText { get; private set; }
        // TODO: probably have to make this a List<string> CommentText ??? 
        public string CommentText { get; private set; }
        public LineType LineType { get; private set; }
        public CommentType CommentType { get; private set; }
        public BlockCommentType BlockCommentType { get; private set; }
        public LineEndCommentType LineEndCommentType { get; private set; }
        public List<Token> Tokens { get; }
        public IDirective Directive { get; private set; }

        public bool IsComment => this.LineType.HasFlag(LineType.ContainsComments);
        public bool IsBlockComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.BlockComment);
        public bool IsLineEndComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.LineEndComment);

        public Line(string sourceFile, int lineNumber, string rawContent)
        {
            this.SourceLocation = new Stack<string>();
            this.SourceLocation.Push(sourceFile);

            this.LineNumber = lineNumber;
            this.RawContent = rawContent;
            this.Tokens = new List<Token>();

            this.ParseLine();
        } 

        public Line(Stack<string> sourceLocation, int lineNumber, string rawContent)
        {
            this.SourceLocation = sourceLocation;

            this.LineNumber = lineNumber;
            this.RawContent = rawContent;
            this.Tokens = new List<Token>();

            this.ParseLine();
        }

        private void ParseLine()
        {
            this.LineType = LineType.RawContent;
            this.CommentType = CommentType.None;

            try
            {
                if (string.IsNullOrWhiteSpace(this.RawContent))
                {
                    this.LineType |= LineType.WhiteSpaceOnly;
                    return;
                }

                var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((ROOT|OUTPUT|FILEMARKER|VERSION_CHECKER|DIRECTORY|FILE|COMMENT|:))|[:]{1})\s*", Global.RegexOptions);
                Match m = regex.Match(this.RawContent);
                if (m.Success)
                {
                    this.LineType = LineType.Directive;

                    var directive = m.Groups["directive"];

                    string directiveName = directive.Value.ToUpperInvariant();
                    int index = directive.Index;

                    Location location = new Location(this.SourceLocation, this.LineNumber, index);

                    IDirective instance = DirectiveFactory.CreateDirective(directiveName, this, location);
                    this.Directive = instance;

                    return;
                }

                regex = new Regex(@"\{\{##(?<token>[^\}\}]+)\}\}", Global.RegexOptions);
                var matches = regex.Matches(this.RawContent);
                if (matches.Count > 0 && matches[0].Success)
                {
                    this.LineType |= LineType.ContainsTokens;  // NOTE that currently, this allows for combinations of RawContent | Directives to be 'decorated' with TokenizedContent... 

                    foreach (Match x in matches)
                    {
                        string tokenData = x.Groups["token"].Value;
                        int index = this.RawContent.IndexOf(tokenData, StringComparison.Ordinal) - 4;
                        Location location = new Location(this.SourceLocation, this.LineNumber, index);

                        Token i = new Token(tokenData, location);
                        this.Tokens.Add(i);
                    }
                }

                regex = new Regex(@"(?<comment>/\*.*?\*/)", Global.RegexOptions);
                matches = regex.Matches(this.RawContent);
                if (matches.Count > 0 && matches[0].Success)
                {
                    this.LineType |= LineType.ContainsComments;
                    this.CommentType = CommentType.BlockComment;

                    string codeWithoutFullyFormedBlockComments = this.RawContent;
                    int commentsCount = 0;
                    foreach (Match x in matches)
                    {
                        string blockComment = x.Groups["comment"].Value;
                        codeWithoutFullyFormedBlockComments = codeWithoutFullyFormedBlockComments.Replace(blockComment, "", StringComparison.InvariantCultureIgnoreCase);
                        commentsCount++;
                    }

                    if (commentsCount == 1)
                    {
                        int lineLength = this.RawContent.TrimEnd().Length;
                        int commentEnd = matches[0].Groups["comment"].Index + matches[0].Groups["comment"].Length;

                        if (lineLength == commentEnd)
                        {
                            if(!string.IsNullOrWhiteSpace(codeWithoutFullyFormedBlockComments))
                                this.BlockCommentType = BlockCommentType.EolComment;
                        }
                        else
                            this.BlockCommentType = BlockCommentType.MidlineComment;
                    }

                    if (commentsCount > 1)
                        this.BlockCommentType = BlockCommentType.MultipleSingleLineComments;

                    // Modifiers: 
                    if (codeWithoutFullyFormedBlockComments == "")
                        this.BlockCommentType |= BlockCommentType.CommentOnly;

                    if (string.IsNullOrWhiteSpace(codeWithoutFullyFormedBlockComments))
                        this.BlockCommentType |= BlockCommentType.WhiteSpaceAndComment;

                    if (codeWithoutFullyFormedBlockComments.Contains("/*"))
                    {
                        this.BlockCommentType |= BlockCommentType.MultiLineStart;
                        this.BlockCommentType |= BlockCommentType.MultipleSingleLineComments; // this is now true too... 
                        this.BlockCommentType |= BlockCommentType.EolComment;  // ditto... this is now true too... 

                        codeWithoutFullyFormedBlockComments = codeWithoutFullyFormedBlockComments.Substring(0, codeWithoutFullyFormedBlockComments.IndexOf("/*"));

                        if(string.IsNullOrWhiteSpace(codeWithoutFullyFormedBlockComments))
                            this.BlockCommentType |= BlockCommentType.WhiteSpaceAndComment;
                    }

                    // TODO: assign .CommentText and .CodeText
                    this.CodeText = codeWithoutFullyFormedBlockComments;

                }
                else
                {
                    if (this.RawContent.Contains("/*", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.LineType |= LineType.ContainsComments;
                        this.CommentType = CommentType.BlockComment;

                        this.BlockCommentType = BlockCommentType.MultiLineStart;
                        this.BlockCommentType |= BlockCommentType.EolComment;

                        string codeWithoutPartiallyFormedBlockComment = this.RawContent.Substring(0, this.RawContent.IndexOf("/*"));
                        if (string.IsNullOrWhiteSpace(codeWithoutPartiallyFormedBlockComment))
                            this.BlockCommentType |= BlockCommentType.WhiteSpaceAndComment;
                    }

                    if (this.RawContent.Contains("*/", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.LineType |= LineType.ContainsComments;
                        this.CommentType = CommentType.BlockComment;

                        this.BlockCommentType = BlockCommentType.MultilineEnd;
                    }
                }

                regex = new Regex(@"(?<comment>--[^\n\r]*)", Global.RegexOptions);
                m = regex.Match(this.RawContent);
                if (m.Success)
                {
                    this.LineType |= LineType.ContainsComments;

                    if (this.CommentType == CommentType.None)
                        this.CommentType = CommentType.LineEndComment;
                    else
                        this.CommentType |= CommentType.LineEndComment;
                    
                    var comment = m.Groups["comment"].Value;

                    var textBeforeLineEndComment = this.RawContent.Substring(0, this.RawContent.IndexOf(comment));

                    // comment type: 
                    if (textBeforeLineEndComment == "")
                        this.LineEndCommentType = LineEndCommentType.FullLineComment;
                    else
                    {
                        if (string.IsNullOrWhiteSpace(textBeforeLineEndComment))
                            this.LineEndCommentType = LineEndCommentType.WhiteSpaceAndComment;
                        else
                            this.LineEndCommentType = LineEndCommentType.EolComment;
                    }

                    this.CodeText = textBeforeLineEndComment;
                    this.CommentText = comment;
                }
            }
            catch (Exception ex)
            {
                // TODO: make sure to throw info about the LINE in question... (i.e., the 'stack' of source details - and the line-number (and column - when possible)
                // And... don't throw... add to .Errors... 
                throw ex;
            }
        }
    }

    public class LinesProcessingResult
    {
        public List<IError> Errors { get; }
        public List<Token> Tokens { get; set; }
        public List<IDirective> Directives { get; }
        public List<Line> Lines { get; }

        public void AddErrors(List<IError> errors)
        {
            this.Errors.AddRange(errors);
        }

        public void AddLine(Line added)
        {
            this.Lines.Add(added);
        }

        public void AddLines(List<Line> added)
        {
            this.Lines.AddRange(added);
        }

        public void AddTokens(List<Token> tokens)
        {
            this.Tokens.AddRange(tokens);
        }

        public void AddDirective(IDirective directive)
        {
            this.Directives.Add(directive);
        }

        public LinesProcessingResult()
        {
            this.Errors = new List<IError>();
            this.Tokens = new List<Token>();
            this.Directives = new List<IDirective>();
            this.Lines= new List<Line>();
        }
    }

    public class LineProcessor
    {
        public static LinesProcessingResult TransformLines(string sourceFile, ProcessingType processingType, IFileManager fileManager, string workingDirectory, string root)
        {
            var output = new LinesProcessingResult();

            int i = 1;
            foreach (string current in fileManager.GetFileLines(sourceFile))
            {
                var line = new Line(sourceFile, i, current);

                if (line.LineType.HasFlag(LineType.ContainsTokens))
                    output.AddTokens(line.Tokens);

                if (line.LineType.HasFlag(LineType.Directive))
                {
                    if (processingType == ProcessingType.IncludedFile)
                    {
                        if (line.Directive.DirectiveName == "FILE")
                        {
                            var nestedSourceFile = new IncludedFile((IncludeFileDirective)line.Directive, fileManager, workingDirectory, root);
                            if (nestedSourceFile.Errors.Count > 0)
                                output.Errors.AddRange(nestedSourceFile.Errors);
                            else
                            {
                                var recursiveResult = LineProcessor.TransformLines(nestedSourceFile.SourceFiles[0],
                                    processingType, fileManager, workingDirectory, root);

                                if (recursiveResult.Errors.Count > 0)
                                    output.AddErrors(recursiveResult.Errors);

                                output.AddLines(recursiveResult.Lines);
                                continue;
                            }
                        }
                        else
                        {
                            if (line.Directive.DirectiveName == "DIRECTORY")
                            {
                                var nestedDirectory = new IncludedDirectory((IncludeDirectoryDirective)line.Directive, fileManager, workingDirectory, root);
                                if(nestedDirectory.Errors.Count > 0)
                                    output.AddErrors(nestedDirectory.Errors);
                                else
                                {
                                    foreach (var nestedSourceFile in nestedDirectory.SourceFiles)
                                    {
                                        var recursiveResult = LineProcessor.TransformLines(nestedSourceFile,
                                            processingType, fileManager, workingDirectory, root);

                                        if(recursiveResult.Errors.Count > 0)
                                            output.AddErrors(recursiveResult.Errors); 

                                        output.AddLines(recursiveResult.Lines);
                                    }

                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        output.AddDirective(line.Directive);
                    }
                }

                output.AddLine(line);
                i++;
            }

            if (processingType == ProcessingType.IncludedFile)
            {
                foreach (var line in output.Lines)
                {
                    // 1. Process 'strings'. 
                    //      and, to be sure: I don't CARE about strings - except for cases like N'CREATE OR ALTER ... ' and/or N'/* this looks like a comment - but it''s actually CODE'... 

                    // TODO: should I move these into Line() itself - to watch for them (i.e., have them IDENTIFIED before getting here?)
                    //      I THINK so ... cuz, that'd make the behavior for strings, comments be the same... (and give me a bit of a framework for object names too.)
                    // strings regex: (\x27)((?!\1).|\1{2})*\1 
                    //      actually: (?<string>(\x27)((?!\1).|\1{2})*\1) 



                    // 2. process comments
                    //      need to process these for 3x reasons: 
                    //          a. need to both REMOVE them based on BuildPreferences 
                    //          b. they can/will contain .DOCUMENTATION 
                    //          c. /* this might look like a CREATE TABLE ... but it's a comment - so don't confuse/conflate the object name with comments */

                    // 3. object names. 
                    //      arguably, if/when we're NOT in the build.sql file (i.e., original file) and IF we're not in a 'string' or /* comment */ (or --comment)... 
                    //          then ... i could potentially use \xxx\file_name.sql to yield <file_name> as a default/place-holder for the object name. 
                    //      OTHERWISE, the object name could/would should be "" or any kind of CREATE|ALTER X|Y|Z type of declaration that I care about... 
                    //          and... i don't know why anyone WOULD do this, but I need to account for STUPID syntax like CREATE OR ALTER<CRLF>FUNC|TABLE|WHATEVER<CRLF>name... and such. 

                }
            }

            return output;
        }
    }
}