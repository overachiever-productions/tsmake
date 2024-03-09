using System;
using System.Runtime.CompilerServices;

namespace tsmake.models
{
    public class Comment
    {

    }

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
        public string CommentText { get; }
        public LineType LineType { get; private set; }
        public List<Token> Tokens { get; }
        public IDirective Directive { get; private set; }
        public Comment Comment { get; }

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

            try
            {
                if (string.IsNullOrWhiteSpace(this.RawContent))
                {
                    this.LineType |= LineType.WhitespaceOnly;
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
                    this.LineType |= LineType.TokenizedContent;  // NOTE that currently, this allows for combinations of RawContent | Directives to be 'decorated' with TokenizedContent... 

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
                    this.LineType |= LineType.BlockComment;

                    string codeSansComment = this.RawContent;
                    int commentsCount = 0;
                    foreach (Match x in matches)
                    {
                        string blockComment = x.Groups["comment"].Value;
                        codeSansComment = codeSansComment.Replace(blockComment, "", StringComparison.InvariantCultureIgnoreCase);
                        commentsCount++;
                    }

                    if (codeSansComment.Contains("/*"))
                    {
                        this.LineType |= LineType.MultipleBlockComments;
                        this.LineType |= LineType.BlockCommentStart;
                    }

                    if (commentsCount > 1)
                        this.LineType |= LineType.MultipleBlockComments;

                    if (string.IsNullOrWhiteSpace(codeSansComment))
                        this.LineType |= LineType.BlockCommentOnly;
                }
                else
                {
                    if (this.RawContent.Contains("/*", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // TODO: COUNT how many /* we have ... in case we're somehow nested... 
                        this.LineType |= LineType.BlockCommentStart;

                    }
                    if (this.RawContent.Contains("*/", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.LineType |= LineType.BlockCommentEnd;
                    }
                }

                regex = new Regex(@"(?<comment>--[^\n\r]*)", Global.RegexOptions);
                m = regex.Match(this.RawContent);
                if (m.Success)
                {
                    this.LineType |= LineType.SimpleComment;
                    var comment = m.Groups["comment"].Value;

                    var preComment = this.RawContent.Substring(0, this.RawContent.IndexOf(comment));
                    if (string.IsNullOrWhiteSpace(preComment))
                        this.LineType |= LineType.SimpleCommentOnly;
                    else
                        this.CodeText = preComment;
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
        public List<Comment> Comments { get; }

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
            this.Comments = new List<Comment>();
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

                if (line.LineType.HasFlag(LineType.TokenizedContent))
                    output.AddTokens(line.Tokens);

                if (line.LineType.HasFlag(LineType.Directive))
                {
                    if (processingType == ProcessingType.IncludedFile)
                    {
                        if (line.Directive.DirectiveName == "FILE")
                        {
                            // TODO: nestedSourceFiles (and Directories) should handle file/path validation ... 
                            //  and if their paths add a Parser or Build Error...  (depending upon whether the directive is mal-formed/wrong (parser error) or ... whether the file isn't found (runtime/build error).
                            var nestedSourceFile = new IncludedFile((IncludeFileDirective)line.Directive, fileManager, workingDirectory, root);

                            var recursiveResult = LineProcessor.TransformLines(nestedSourceFile.SourceFiles[0], processingType, fileManager, workingDirectory, root);

                            if (recursiveResult.Errors.Count > 0)
                                output.AddErrors(recursiveResult.Errors);

                            output.AddLines(recursiveResult.Lines);
                            continue;
                        }
                        else
                        {
                            if (line.Directive.DirectiveName == "DIRECTORY")
                            {
                                // TODO: create a new 'Directory' object ... via directives and such... 
                                // then, for EACH nestedDirectorySourceFile ... 
                                //      get the processingResult
                                //      add errors 
                                //      add lines... 
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



            // TODO: I'm not sure if these (both) should ALWAYS be done or not... 
            //if (processStrings)
            //{
            //          i.e., look for things like N'CREATE or ALTER X ... ' and N'/* this looks like a comment, but isn''t';
            //}
            
            //if (processComments)
            //{

            //}

            //if (processObjectOwner)
            //{

            //}

            return output;
        }
    }
}