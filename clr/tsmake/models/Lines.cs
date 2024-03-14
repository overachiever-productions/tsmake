using System.Diagnostics;

namespace tsmake.models
{
    public class Location
    {
        public string FileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        public Location(string fileName, int lineNumber, int columnNumber = 0)
        {
            this.FileName = fileName;
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
        }

        //public string GetLocationContext()
        //{
        //    //return $"Location: [{this.FileName}]({this.LineNumber}, {this.ColumnNumber})";
        //    return "TODO: need to chain/concat full-source-file lineage thingies in Location.GetLocationContext().";
        //}
    }

    public interface ILineDecorator
    {
        public Stack<Location> Location { get; }
        public string Text { get; }
        public int LineStart { get; }
        public int ColumnStart { get; }
        public int LineEnd { get; }
        public int ColumnEnd { get; }
    }

    public abstract class BaseLineDecorator : ILineDecorator
    {
        public Stack<Location> Location { get; }
        public string Text { get; }
        public int LineStart { get; }
        public int ColumnStart { get; }
        public int LineEnd { get; }
        public int ColumnEnd { get; }

        protected BaseLineDecorator(Stack<Location> location, int lineStart, int columnStart)
        {
            this.Location = location;
            this.LineStart = lineStart;
            this.ColumnStart = columnStart;
        }

        protected BaseLineDecorator(Stack<Location> location, string text, int lineStart, int columnStart, int lineEnd, int columnEnd)
        {
            this.Location = location;
            this.Text = text;
            this.LineStart = lineStart;
            this.ColumnStart = columnStart;
            this.LineEnd = lineEnd;
            this.ColumnEnd = columnEnd;
        }

        protected void Close(string text, int lineEnd, int columnEnd)
        {

        }
    }

    // represents either -- comments (till end of line (pretty simple) or /* comments 
    //          which can span multiple lines and a bunch of other stuff... */ 
    public class CodeComment : BaseLineDecorator
    {
        public CodeComment(Stack<Location> location, int lineStart, int lineEnd) : base(location, lineStart, lineEnd) { }
        public CodeComment(Stack<Location> location, string commentText, int lineStart, int columnStart, int lineEnd, int columnEnd)
            : base(location, commentText, lineStart, columnStart, lineEnd, columnEnd) { }

        public void CloseMultiLineComment(string fullCommentText, int lineEnd, int columnEnd)
        {
            base.Close(fullCommentText, lineEnd, columnEnd);
        }
    }

    //public class CodeString : BaseLineDecorator
    //{
    //    // represents 'string comments - single or multi-line and so on'... 
    //}

    //public class ObjectDefinition : BaseLineDecorator
    //{
    //    // this should extend the interface with 2x properties: 
    //    //  .ObjectName 
    //    //  .DefaultName (which is the filename)

    //    // i THINK this makes sense... but, assume that we've got something like ALTER|CREATE (whitelisted|object|type|here)... 
    //    //      at which point... this 'goes' until we hit a GO ... or ... find another ALTER|CREATE further down...
    //    //      right? at which point, the .DecoratorText is the ENTIRE friggin object definition. 
    //    //          and... by default... i think I should probably set some sort of .DefaultName ... which is the file-name in question.
    //}

    public class Line
    {
        public Stack<Location> Location { get; private set; }
        public int LineNumber { get; }
        public string RawContent { get; }
        public string CodeOnlyText { get; private set; }
        public List<CodeComment> CodeComments { get; private set; }
        public LineType LineType { get; private set; }
        public CommentType CommentType { get; private set; }
        public BlockCommentType BlockCommentType { get; private set; }
        public LineEndCommentType LineEndCommentType { get; private set; }
        public List<Token> Tokens { get; }
        public IDirective Directive { get; private set; }

        public bool IsComment => this.LineType.HasFlag(LineType.ContainsComments);
        public bool IsBlockComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.BlockComment);
        public bool IsLineEndComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.LineEndComment);

        public string GetCommentText()
        {
            if(!this.IsComment)
                return string.Empty;
            if (this.CodeComments.Count == 1)
            {
                return this.CodeComments[0].Text;
            }
            else
            {
                return "compound comments go here... ";
            }
        }

        public string GetLocation(string indent = "\t", bool increaseIndent = true)
        {
            if (this.Location.Count == 1)
            {
                return this.Location.Peek().FileName + ", " + this.Location.Peek().LineNumber;
            }

            var sb = new StringBuilder();
            var locationClone = this.Location.Clone();
            var currentIndent = indent;
            if (increaseIndent) currentIndent = "";
            while (locationClone.Count > 0)
            {
                var location = locationClone.Pop();
                sb.AppendLine($"{currentIndent}{location.FileName} , {location.LineNumber}");
                if (increaseIndent)
                    currentIndent += indent;
            }

            return sb.ToString();
        }

        public Line(string sourceFile, int lineNumber, string rawContent)
        {
            this.Location = new Stack<Location>();

            Location current = new Location(sourceFile, lineNumber);
            this.Location.Push(current);

            this.LineNumber = lineNumber;
            this.RawContent = rawContent;
            this.Tokens = new List<Token>();

            this.ParseLine();
        }

        public Line(Line parent, string sourceFile, int lineNumber, string rawContent)
        {
            this.Location = new Stack<Location>();
            var stackClone = parent.Location.Clone();
            while (stackClone.Count > 0)
                this.Location.Push(stackClone.Pop());

            this.Location.Push(new Location(sourceFile, lineNumber));

            this.LineNumber = lineNumber;
            this.RawContent = rawContent;
            this.Tokens = new List<Token>();

            this.ParseLine();
        }

        private void ParseLine()
        {
            this.CodeComments = new List<CodeComment>();
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

                    // TODO: verify that this makes sense... 
                    Location location = new Location(this.Location.Peek().FileName, this.LineNumber, index);

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
                        Location location = new Location(this.Location.Peek().FileName, this.LineNumber, index);

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
                        int start = x.Groups["comment"].Index;
                        int length = x.Groups["comment"].Length;

                        this.CodeComments.Add(new CodeComment(this.Location, blockComment, this.LineNumber, start, this.LineNumber, start + length));

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

                    // TODO: assign .CommentText and .CodeOnlyText
                    this.CodeOnlyText = codeWithoutFullyFormedBlockComments;

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

                    this.CodeOnlyText = textBeforeLineEndComment;
                    this.CodeComments.Add(new CodeComment(this.Location, comment, this.LineNumber, m.Index, this.LineNumber, this.RawContent.Length));
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
        public static LinesProcessingResult ProcessLines(Line parent, string currentSourceFile, ProcessingType processingType, IFileManager fileManager, string workingDirectory, string root)
        {
            var output = new LinesProcessingResult();

            int i = 1;
            foreach (string current in fileManager.GetFileLines(currentSourceFile))
            {
                var line = (parent == null) ? new Line(currentSourceFile, i, current) : new Line(parent, currentSourceFile, i, current);

                if (line.LineType.HasFlag(LineType.ContainsTokens))
                    output.AddTokens(line.Tokens);

                if (line.LineType.HasFlag(LineType.Directive))
                {
                    if (processingType == ProcessingType.IncludedFile)
                    {
                        if (line.Directive.DirectiveName == "FILE")
                        {
                            var nestedFile = new IncludedFile((IncludeFileDirective)line.Directive, fileManager, workingDirectory, root);
                            if (nestedFile.Errors.Count > 0)
                                output.Errors.AddRange(nestedFile.Errors);
                            else
                            {
                                var nestedSourceFile = nestedFile.SourceFiles[0];
                                var recursiveResult = LineProcessor.ProcessLines(line, nestedSourceFile, processingType, fileManager, workingDirectory, root);

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
                                        var recursiveResult = LineProcessor.ProcessLines(line, nestedSourceFile, processingType, fileManager, workingDirectory, root);

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
                    // NOTE: For determining if comments are inside of 'single ticks (strings)' and whether object-names are within /* comments */ or 'ticks'... 
                    //      I don't have to get super pedantic about where potential-outer-strings MIGHT start and whether the start/end 'wraps' the thing i'm looking for. 
                    // instead, if I have ALL string text - for example - and find that --comments or /* comments */ exist on/in the same line (or lines) as 'string data'... 
                    //      i just need to do something like foreach(s in this.line's.text-string) { if string.contains(comment) this.commentIsInString = true; } 
                    //      etc... 





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