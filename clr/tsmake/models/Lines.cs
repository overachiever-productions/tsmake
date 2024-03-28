namespace tsmake.models
{
    public class Position
    {
        public int LineNumber { get; }
        // REFACTOR: I'm calling these ColumnNumbers - which should be 1 based. 
        //      but they're ACTUALLY indexes - i.e., 0 based. 
        //      and I want to keep them 0-based (they're code) ... 
        //      which means: 
        //      1) i need a better name. 
        //      2) IF I end up spitting these out to end-users (for like 'location'-type 'stuff') they'll need to be +1'd 
        //          so that they make sense to end-users. 
        public int ColumnNumber { get; }

        public Position(int lineNumber, int columnNumber)
        {
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
        }
    }

    public class Location
    {
        public string FileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; internal set; }

        public Location(string fileName, int lineNumber, int columnNumber = 0)
        {
            this.FileName = fileName;
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
        }
    }

    public interface ILineDecorator
    {
        public Stack<Location> Location { get; }
        public string Text { get; }
        public string CurrentLineText { get; }
        public int LineStart { get; }
        public int ColumnStart { get; }
        public int LineEnd { get; }
        public int ColumnEnd { get; }
        public Guid Identifier { get; }
    }

    public abstract class BaseLineDecorator : ILineDecorator
    {
        public Stack<Location> Location { get; private set; }
        public string Text { get; }
        public string CurrentLineText { get; private set; }
        public int LineStart { get; }
        public int ColumnStart { get; }
        public int LineEnd { get; private set; }
        public int ColumnEnd { get; private set; }
        public Guid Identifier { get; }

        protected BaseLineDecorator(string text, int lineStart, int columnStart)
        {
            this.Text = text;
            this.LineStart = lineStart;
            this.ColumnStart = columnStart;

            this.Identifier = Guid.NewGuid();
            this.Location = new Stack<Location>();
        }

        protected BaseLineDecorator(string text, int lineStart, int columnStart, int lineEnd, int columnEnd) : this(text, lineStart, columnStart)
        {
            LineEnd = lineEnd;
            ColumnEnd = columnEnd;
        }

        public void SetCurrentLineText(string currentLineText)
        {
            this.CurrentLineText = currentLineText;
        }

        public void SetLocation(Stack<Location> location, int startPosition)
        {
            if (location.Peek().ColumnNumber != startPosition)
            {
                Location old = location.Pop();
                old.ColumnNumber = startPosition;
                location.Push(old);
            }

            this.Location = location;
        }

        public void SetEndPosition(Position endPosition)
        {
            this.LineEnd = endPosition.LineNumber;
            this.ColumnEnd = endPosition.ColumnNumber;
        }

        public override bool Equals(object other)
        {
            if(other == null) return false;

            var otherDecorator = (BaseLineDecorator)other;
            if (otherDecorator.Identifier == this.Identifier)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.Identifier.GetHashCode();
        }
    }

    public class CodeComment : BaseLineDecorator
    {
        public bool CommentIsInString { get; private set; }
        public bool IsHeaderComment { get; private set; }
        public CommentType CommentType { get; internal set; }
        public BlockCommentType BlockCommentType { get; internal set; }
        // I don't think I need this: 
        //public LineEndCommentType LineEndCommentType { get; internal set; }

        public CodeComment(string text, int lineStart, int columnStart) : base(text, lineStart, columnStart) { }

        public CodeComment(string text, int lineStart, int columnStart, int lineEnd, int columnEnd) : base(text,
            lineStart, columnStart, lineEnd, columnEnd)
        {
            this.CommentIsInString = false;
            this.IsHeaderComment = false;  
        }

        public CodeComment AsCopy()
        {
            return (CodeComment)this.MemberwiseClone();
        }

        public void MarkCommentAsInString()
        {
            this.CommentIsInString = true;
        }

        public void SetAsHeaderComment()
        {
            this.IsHeaderComment = true;
        }
    }

    public class CodeString : BaseLineDecorator
    {
        public CodeString(string text, int lineStart, int columnStart) : base(text, lineStart, columnStart) { }

        public CodeString AsCopy()
        {
            return (CodeString)this.MemberwiseClone();
        }
    }

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
        public int LineNumber { get; }
        public Stack<Location> Location { get; private set; }
        public string RawContent { get; }
        public List<CodeComment> CodeComments { get; private set; }
        public List<CodeString> CodeStrings { get; private set; }
        
        //public LineType LineType { get; internal set; }
        //public CommentType CommentType { get; internal set; }
        //public BlockCommentType BlockCommentType { get; internal set; }
        // REFACTOR: Do I REALLY need this? The only way that I could see 'needing' it would be IF a line was 100% a --comment (starting at position 0) ... and should, as such, be excluded. BUT, I think i could 'tell' this other ways.
        //public LineEndCommentType LineEndCommentType { get; internal set; }
        //public StringType StringType { get; internal set; }
        
        public List<Token> Tokens { get; }
        public IDirective Directive { get; private set; }

        public bool HasComment => this.LineType.HasFlag(LineType.ContainsComments);
        public bool HasBlockComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.BlockComment);
        public bool HasLineEndComment => this.LineType.HasFlag(LineType.ContainsComments) & this.CommentType.HasFlag(CommentType.LineEndComment);
        public bool IsHeaderComment => this.CodeComments.Any(x => x.IsHeaderComment);
        public bool IsStringNotComment => this.CodeComments.All(x => x.CommentIsInString);

        public bool HasString => this.LineType.HasFlag(LineType.ContainsStrings);

        // REFACTOR: see comments in BuildFileTests.EndOfLine_Comments_And_Block_Comments_Can_Live_Together()
        //      I'm PRETTY sure that I can't end up using THIS method (.GetCodeOnlyText() is great/fine)... because of how
        //          -- comments and /* comments */ interact with each other (and/or because of how I'm handling them).
        public string GetCommentText()
        {
            if(!this.HasComment)
                return string.Empty;

            if (this.CodeComments.Count == 1)
                return this.CodeComments[0].Text;

            string output = "";
            foreach (CodeComment comment in this.CodeComments)
                output += comment.Text;

            return output;
        }

        public string GetCodeOnlyText()
        {
            if (!this.HasComment)
                return this.RawContent;

            // TODO: if comment.CommentIsInString ... then don't replace - output... 
            string output = this.RawContent;
            foreach (var comment in this.CodeComments)
            {
                if(comment.LineStart != comment.LineEnd)
                {
                    var commentLines = Regex.Split(comment.Text, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();
                    int index = this.LineNumber - comment.LineStart;

                    var commentLineText = commentLines[index];

                    output = output.Replace(commentLineText, "");
                }
                else
                    output = output.Replace(comment.Text, "");
            }

            return output;
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
            this.CodeStrings = new List<CodeString>();

            this.LineType = LineType.RawContent;
            //this.CommentType = CommentType.None;

            try
            {
                if (string.IsNullOrWhiteSpace(this.RawContent))
                {
                    this.LineType |= LineType.WhiteSpaceOnly;
                    return;
                }

                var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((ROOT|OUTPUT|FILEMARKER|VERSION_CHECKER|DIRECTORY|FILE|COMMENT|:))|[:]{1})\s*", Global.SingleLineOptions);
                Match m = regex.Match(this.RawContent);
                if (m.Success)
                {
                    this.LineType = LineType.Directive;

                    var directive = m.Groups["directive"];

                    string directiveName = directive.Value.ToUpperInvariant();
                    int index = directive.Index;

                    Location location = new Location(this.Location.Peek().FileName, this.LineNumber, index);

                    IDirective instance = DirectiveFactory.CreateDirective(directiveName, this, location);
                    this.Directive = instance;

                    return;
                }

                regex = new Regex(@"\{\{##(?<token>[^\}\}]+)\}\}", Global.SingleLineOptions);
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

                //regex = new Regex(@"(?<comment>--[^\n\r]*)", Global.SingleLineOptions);
                //m = regex.Match(this.RawContent);
                //if (m.Success)
                //{
                //    this.LineType |= LineType.ContainsComments;
                //    this.CommentType = CommentType.LineEndComment;
                    
                //    var comment = m.Groups["comment"].Value;
                //    var textBeforeLineEndComment = this.RawContent.Substring(0, this.RawContent.IndexOf(comment, StringComparison.InvariantCultureIgnoreCase));

                //    if (textBeforeLineEndComment == "")
                //        this.LineEndCommentType = LineEndCommentType.FullLineComment;
                //    else
                //    {
                //        if (string.IsNullOrWhiteSpace(textBeforeLineEndComment))
                //            this.LineEndCommentType = LineEndCommentType.WhiteSpaceAndComment;
                //        else
                //            this.LineEndCommentType = LineEndCommentType.EolComment;
                //    }

                //    var codeComment = new CodeComment(m.Value, this.LineNumber, m.Index, this.LineNumber, (m.Index + m.Length - 1));
                //    codeComment.SetLocation(this.Location, m.Index);

                //    this.CodeComments.Add(codeComment);
                //}
            }
            catch (Exception ex)
            {
                // TODO: make sure to throw info about the LINE in question... (i.e., the 'stack' of source details - and the line-number (and column - when possible)
                // And... don't throw... add to .Errors... 
                throw ex;
            }
        }
    }

    public class FileProcessingResult
    {
        public List<IError> Errors { get; }
        public List<Token> Tokens { get; set; }
        public List<IDirective> Directives { get; }
        public List<CodeComment> Comments {get; private set; }
        public List<CodeString> CodeStrings { get; private set; }
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

        public void AddCodeComment(CodeComment comment)
        {
            this.Comments.Add(comment);
        }

        public void AddCodeString(CodeString codeString)
        {
            this.CodeStrings.Add(codeString);
        }

        public FileProcessingResult()
        {
            this.Errors = new List<IError>();
            this.Tokens = new List<Token>();
            this.Directives = new List<IDirective>();
            this.Comments = new List<CodeComment>();
            this.CodeStrings = new List<CodeString>();
            this.Lines= new List<Line>();
        }
    }

    public class FileProcessor
    {
        public static FileProcessingResult ProcessFileLines(Line parent, string currentSourceFile, ProcessingType processingType, IFileManager fileManager, string workingDirectory, string root)
        {
            var output = new FileProcessingResult();

            List<string> fileBodyLines = fileManager.GetFileLines(currentSourceFile);

            int i = 1;
            foreach (string current in fileBodyLines)
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
                                var recursiveResult = FileProcessor.ProcessFileLines(line, nestedSourceFile, processingType, fileManager, workingDirectory, root);

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
                                        var recursiveResult = FileProcessor.ProcessFileLines(line, nestedSourceFile, processingType, fileManager, workingDirectory, root);

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

                if (line.LineType.HasFlag(LineType.ContainsComments))
                    output.Comments.Add(line.CodeComments[0]); // there 'can only be one' cuz these are end-of-line-comments only/ever. 

                output.AddLine(line);
                i++;
            }

            string fileBody = fileManager.GetFileContent(currentSourceFile);

            var regex = new Regex(@"(?<string>N?(\x27)((?!\1).|\1{2})*\1)", Global.SingleLineOptions);
            var matches = regex.Matches(fileBody);
            foreach (Match match in matches)
            {
                int index = match.Index;
                int length = match.Length;
                string stringText = match.Groups["string"].Value;

                Position startPosition = FileProcessor.GetFilePositionByCharacterIndex(fileBody, index);
                Position endPosition;

                Line startLine = output.Lines[startPosition.LineNumber - 1];
                var codeString = new CodeString(stringText, startPosition.LineNumber, startPosition.ColumnNumber);

                if (Regex.IsMatch(stringText, @"\r\n|\r|\n", Global.SingleLineOptions))
                {
                    int matchEnd = index + length - 1;
                    endPosition = FileProcessor.GetFilePositionByCharacterIndex(fileBody, matchEnd);
                    codeString.SetEndPosition(endPosition);

                    startLine.LineType |= LineType.ContainsStrings;
                    startLine.StringType = StringType.MultiLine;
                    startLine.StringType |= StringType.MultiLineStart;

                    string[] stringTextLines = Regex.Split(stringText, @"\r\n|\r|\n", Global.SingleLineOptions);
                    string currentStringTextLine = stringTextLines[0];
                    codeString.SetCurrentLineText(currentStringTextLine);

                    startLine.CodeStrings.Add(codeString.AsCopy());

                    // See logic notes for /* comments */ (about 'off by 1 - but really not' types of details / etc.)
                    for (int x = startLine.LineNumber; x <= endPosition.LineNumber - 2; x++)
                    {
                        Line currentLine = output.Lines[x];

                        currentLine.LineType |= LineType.ContainsStrings;
                        currentLine.StringType = StringType.MultiLine;
                        currentLine.StringType |= StringType.MultiLineLine;
                        
                        currentStringTextLine = stringTextLines[(x - startLine.LineNumber + 1)];
                        codeString.SetCurrentLineText(currentStringTextLine);

                        currentLine.CodeStrings.Add(codeString.AsCopy());
                    }

                    if (startPosition.LineNumber != endPosition.LineNumber)
                    {
                        Line endLine = output.Lines[endPosition.LineNumber - 1];
                        endLine.LineType |= LineType.ContainsStrings;
                        endLine.StringType = StringType.MultiLine;
                        endLine.StringType |= StringType.MultiLineEnd;

                        currentStringTextLine = stringTextLines[stringTextLines.Length - 1];
                        codeString.SetCurrentLineText(currentStringTextLine);

                        endLine.CodeStrings.Add(codeString.AsCopy());
                    }
                }
                else
                {
                    endPosition = new Position(startPosition.LineNumber, startPosition.ColumnNumber + length - 1);
                    codeString.SetEndPosition(endPosition);

                    startLine.LineType |= LineType.ContainsStrings;
                    startLine.StringType = StringType.SingleLine;

                    codeString.SetCurrentLineText(stringText);
                    startLine.CodeStrings.Add(codeString.AsCopy());
                }

                codeString.SetLocation(startLine.Location, startPosition.ColumnNumber);

                output.AddCodeString(codeString);
            }

            regex = new Regex(@"(?<comment>/\*.*?\*/)", Global.SingleLineOptions);
            matches = regex.Matches(fileBody);
            foreach (Match match in matches)
            {
                int index = match.Index;
                int length = match.Length;
                string commentText = match.Groups["comment"].Value;

                Position startPosition = FileProcessor.GetFilePositionByCharacterIndex(fileBody, index);
                Position endPosition;

                Line startLine = output.Lines[startPosition.LineNumber - 1];
                var codeComment = new CodeComment(commentText, startPosition.LineNumber, startPosition.ColumnNumber);

                if (Regex.IsMatch(commentText, @"\r\n|\r|\n", Global.SingleLineOptions))
                {
                    int matchEnd = index + length - 1;
                    endPosition = FileProcessor.GetFilePositionByCharacterIndex(fileBody, matchEnd);
                    codeComment.SetEndPosition(endPosition);

                    startLine.LineType |= LineType.ContainsComments;
                    
                    // REFACTOR: used to have to 'check' this when --comments were processed BEFORE /* comments */
                    if(startLine.CommentType == CommentType.None) 
                        startLine.CommentType = CommentType.BlockComment;
                    else 
                        startLine.CommentType |= CommentType.BlockComment;
                        
                    startLine.BlockCommentType |= BlockCommentType.MultiLineStart;

                    string[] commentTextLines = Regex.Split(commentText, @"\r\n|\r|\n", Global.SingleLineOptions);
                    string currentCommentTextLine = commentTextLines[0];
                    codeComment.SetCurrentLineText(currentCommentTextLine);

                    startLine.CodeComments.Add(codeComment.AsCopy());

                    // LOGIC: Don't want to use startLine2.LineNumber - 1 (that's the START) - i.e., the 'off by one' here bumps us to next line. 
                    //          Ditto on endPosition.LineNumber. Don't want that to be -1 (that'd be zero-based index of endPosition). We want
                    //          the line BEFORE that (i.e., -2)
                    for (int x = startLine.LineNumber; x <= endPosition.LineNumber - 2; x++)
                    {
                        Line currentLine = output.Lines[x];
                        
                        currentLine.LineType |= LineType.ContainsComments;

                        // REFACTOR: used to have to 'check' this when --comments were processed BEFORE /* comments */
                        if (currentLine.CommentType == CommentType.None)
                            currentLine.CommentType = CommentType.BlockComment;
                        else
                            currentLine.CommentType |= CommentType.BlockComment;
                        
                        currentLine.BlockCommentType |= BlockCommentType.MultilineLine;
                        currentLine.BlockCommentType |= BlockCommentType.CommentOnly;

                        currentCommentTextLine = commentTextLines[(x - startLine.LineNumber + 1)];
                        codeComment.SetCurrentLineText(currentCommentTextLine);
                        currentLine.CodeComments.Add(codeComment.AsCopy());
                    }

                    if (startPosition.LineNumber != endPosition.LineNumber)
                    {
                        Line endLine = output.Lines[endPosition.LineNumber - 1];
                        endLine.LineType |= LineType.ContainsComments;

                        // REFACTOR: used to have to 'check' this when --comments were processed BEFORE /* comments */
                        if (endLine.CommentType == CommentType.None)
                            endLine.CommentType = CommentType.BlockComment;
                        else
                            endLine.CommentType |= CommentType.BlockComment;

                        endLine.BlockCommentType |= BlockCommentType.MultilineEnd;

                        currentCommentTextLine = commentTextLines[commentTextLines.Length - 1];
                        codeComment.SetCurrentLineText(currentCommentTextLine);
                        endLine.CodeComments.Add(codeComment.AsCopy());
                    }
                }
                else
                {
                    endPosition = new Position(startPosition.LineNumber, startPosition.ColumnNumber + length - 1);
                    codeComment.SetEndPosition(endPosition);

                    startLine.LineType |= LineType.ContainsComments;

                    // REFACTOR: used to have to 'check' this when --comments were processed BEFORE /* comments */
                    if (startLine.CommentType == CommentType.None)
                        startLine.CommentType = CommentType.BlockComment;
                    else
                        startLine.CommentType |= CommentType.BlockComment;

                    startLine.BlockCommentType |= BlockCommentType.SingleLine;
                    
                    codeComment.SetCurrentLineText(commentText);
                    startLine.CodeComments.Add(codeComment.AsCopy());
                }

                codeComment.SetLocation(startLine.Location, startPosition.ColumnNumber);

                // push a copy of each comment into the collection of comments by file/processing-result as well:
                output.AddCodeComment(codeComment);
            }

            // Now that strings/comments have been identified:
            // 0. line-end comments,  1. look for overlap of 'strings and /* comments or --comments */', 2. identify header comments, 3. object names

            bool isHeaderComment = true;
            List<CodeComment> lineCommentsToRemove = new List<CodeComment>();
            foreach (var line in output.Lines)
            {
                regex = new Regex(@"(?<comment>--[^\n\r]*)", Global.SingleLineOptions);
                var m = regex.Match(line.RawContent);
                if (m.Success)
                {
                    var comment = m.Groups["comment"].Value;

                    bool eolCommentIsInString = false;
                    if (line.HasString)
                    {
                        var trimmedToEndOfStringComment = comment;
                        if(comment.Contains("'"))
                            trimmedToEndOfStringComment = comment.Substring(0, comment.LastIndexOf("'"));
                        if (line.CodeStrings.Any(x => x.CurrentLineText.Contains(trimmedToEndOfStringComment)))
                            eolCommentIsInString = true;
                    }

                    bool eolCommentIsInBlockComment = false;
                    if (line.HasBlockComment)
                    {
                        var trimmedToEndOfCommentBlockComment = comment;
                        if(comment.Contains("*/"))
                            trimmedToEndOfCommentBlockComment = comment.Substring(0, comment.LastIndexOf("*/"));
                        if (line.CodeComments.Any(x => x.CurrentLineText.Contains(trimmedToEndOfCommentBlockComment)))
                            eolCommentIsInBlockComment = true;
                    }

                    if (eolCommentIsInString == false && eolCommentIsInBlockComment == false)
                    {
                        line.LineType |= LineType.ContainsComments;

                        if (line.CommentType == CommentType.None)
                            line.CommentType = CommentType.LineEndComment;
                        else
                            line.CommentType |= CommentType.LineEndComment;

                        var textBeforeLineEndComment = line.RawContent.Substring(0,
                            line.RawContent.IndexOf(comment, StringComparison.InvariantCultureIgnoreCase));

                        if (textBeforeLineEndComment == "")
                            line.LineEndCommentType = LineEndCommentType.FullLineComment;
                        else
                        {
                            if (string.IsNullOrWhiteSpace(textBeforeLineEndComment))
                                line.LineEndCommentType = LineEndCommentType.WhiteSpaceAndComment;
                            else
                                line.LineEndCommentType = LineEndCommentType.EolComment;
                        }

                        var codeComment = new CodeComment(m.Value, line.LineNumber, m.Index, line.LineNumber,
                            (m.Index + m.Length - 1));
                        codeComment.SetLocation(line.Location, m.Index);
                        codeComment.SetCurrentLineText(comment);

                        line.CodeComments.Add(codeComment);
                        output.Comments.Add(codeComment);
                    }
                }

                // FEATURECREEP: I can either check for line.HasComment or line.HasBlockComment - to establish whether this is a header-comment. 
                //      as in: I think that the determination here MIGHT make sense to make a PREFERENCE. And if so, then just determine whether var isPotentialHeaderComment is true/false based upon whether we're looking for ONLY /* comments */ or --comments and /* comments */
                //      for now, I'm hard-coding as checking for BlockComments only... 
                if (line.HasBlockComment)
                {
                    if(isHeaderComment)
                        line.CodeComments[0].SetAsHeaderComment();
                }
                else
                    isHeaderComment = false;

                // check for 'STRINGS that are --comments or /*comments*/'... 
                if (line.HasString)
                {
                    if (line.HasComment)
                    {
                        foreach (var comment in line.CodeComments)
                        {
                            if (line.CodeStrings.Any(x => x.CurrentLineText.Contains(comment.CurrentLineText)))
                            {
                                // REFACTOR: .MarkCommentAsInString() is ... useless. I can remove it once tests are in place to verify that nothing breaks.
                                comment.MarkCommentAsInString();
                                lineCommentsToRemove.Add(comment);

                                //if (comment.LineStart != comment.LineEnd)
                                //{
                                //    // it's multi-line and ... need to remove it from each line. 
                                //    for (int n = comment.LineStart; n < comment.LineEnd; n++)
                                //    {
                                //        var targetLine = output.Lines[n];

                                //    }
                                //}
                                //else
                                //    line.CodeComments.Remove(comment);

                                output.Comments.Remove(comment);
                            }
                        }
                    }
                }
            }

            // CAN'T remove these from the previous loop - because of the whole "collection was modified" error/thingy. 
            foreach (var commentToRemove in lineCommentsToRemove)
            {
                if (commentToRemove.LineStart != commentToRemove.LineEnd)
                {
                    for (int n = commentToRemove.LineStart; n < commentToRemove.LineEnd; n++)
                    {
                        var targetLine = output.Lines[n - 1];
                        targetLine.CodeComments.Remove(commentToRemove);

                        if (targetLine.CodeComments.Count < 1)
                        {
                            targetLine.LineType &= LineType.ContainsComments;
                            
                            // TODO: and set .CommentType to .None 
                            // and set .BlockCommentType to .None 
                            // and set .EolCommentType to .None 
                        }
                    }
                }
                else
                {
                    var targetLine = output.Lines[commentToRemove.LineStart - 1];
                    targetLine.CodeComments.Remove(commentToRemove);

                    if (targetLine.CodeComments.Count < 1)
                    {
                        targetLine.LineType &= LineType.ContainsComments;
                        // todo... same as above... 
                    }
                }
            }

            return output;
        }

        public static Position GetFilePositionByCharacterIndex(string fileBody, int targetIndex)
        {
            if (targetIndex == 0) return new Position(1, 0);

            int lineNumber = 1;
            int previousNewLineStart = 0;

            var regex = new Regex(@"(\r\n|\r|\n)", Global.SingleLineOptions);
            var lineMatches = regex.Matches(fileBody);
            foreach (Match crlfStart in lineMatches)
            {
                // LOGIC: newLineStart.index is where the [CR][LF] STARTS - so always add that to 'current' position to make sure
                //      we're at the START of the new-line. And, of course, can't just be 'dumb' and do a +2 for the [CR][LF]
                //      because we might be on unix OR have 'inconsistent line endings' in the code file... so, use .length.
                int newlineStartIndex = crlfStart.Index + crlfStart.Length;

                if (targetIndex < newlineStartIndex)
                {
                    int columnIndex = (targetIndex - previousNewLineStart);
                    return new Position(lineNumber, columnIndex);
                }

                lineNumber++;
                previousNewLineStart = newlineStartIndex;
            }

            return new Position(lineNumber, (targetIndex - previousNewLineStart));
        }
    }
}