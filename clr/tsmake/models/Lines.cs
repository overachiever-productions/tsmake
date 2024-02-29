namespace tsmake.models
{
    public class Line
    {
        public int LineNumber { get; }
        public string Content { get; }
        public string Source { get; }
        public LineType LineType { get; set; }
        public List<Token> Tokens { get;}
        //public List<IDirective> Directives { get;}
        public IDirective Directive { get; private set; }

        public Line(int number, string content, string source)
        {
            this.Tokens = new List<Token>();
            //this.Directives = new List<IDirective>();

            this.LineNumber = number;
            this.Content = content;
            this.Source = source;

            this.ParseLine(this);
        }

        private void ParseLine(Line line)
        {
            this.LineType = LineType.RawContent;

            try
            {
                if (string.IsNullOrWhiteSpace(line.Content))
                {
                    this.LineType |= LineType.WhitespaceOnly;
                    return;
                }

                var regex = new Regex(@"^\s*--\s*##\s*(?<directive>(\w+)|[::]{1,})\s*", Global.RegexOptions);
                Match m = regex.Match(line.Content);
                if (m.Success)
                {
                    this.LineType = LineType.Directive;

                    var directive = m.Groups["directive"];

                    string directiveName = directive.Value.ToUpperInvariant();
                    int index = directive.Index;

                    Location location = new Location(line.Source, line.LineNumber, index);

                    IDirective instance = DirectiveFactory.CreateDirective(directiveName, this, location);
                    this.Directive = instance;

                    // TODO: do I return at this point? or can a line have TOKENS in it - even if/when it's a directive? 
                    //      i.e., just need to figure out how I want to parse various syntax rules. 
                    //      because EVEN IF I end up using something like tokens in something like, say, the ## FILEMARKER: ... , I COULD just process those WITHOUT full-blown 'token support'.
                }

                regex = new Regex(@"\{\{##(?<token>[^\}\}]+)\}\}", Global.RegexOptions);
                m = regex.Match(line.Content);
                if(m.Success) { 
                    this.LineType |= LineType.TokenizedContent;  // NOTE that currently, this allows for combinations of RawContent | Directives to be 'decorated' with TokenizedContent... 
                    
                    foreach (Match x in regex.Matches(line.Content))
                    {
                        string tokenData = x.Groups["token"].Value;
                        int index = line.Content.IndexOf(tokenData, StringComparison.Ordinal) - 4;
                        Location location = new Location(line.Source, line.LineNumber, index);

                        Token i = new Token(tokenData, location);
                        this.Tokens.Add(i);
                    }
                }
            }
            catch (Exception ex)
            {
                // make sure to throw info about the LINE in question... 
                throw ex;
            }
        }
    }

    public class SourceFileProcessingResult
    {
        public List<IError> Errors { get; }
        public List<Line> Lines { get; }

        public SourceFileProcessingResult()
        {
            this.Errors = new List<IError>();
            this.Lines = new List<Line>();
        }
    }

    // REFACTOR: maybe call this a SourceFileParser?
    public class LineParser
    {
        private LineParser() { }

        public static LineParser Instance => new LineParser();

        public SourceFileProcessingResult ParseLines(string sourceFile, FileLineage lineage)
        {
            var buffer = new SourceFileProcessingResult();
            
            string current;
            int i = 1; 
            using (var streamReader = new StreamReader(sourceFile))
            {
                while ((current = streamReader.ReadLine()) != null)
                {
                    var line = new Line(i, current, sourceFile);

                    // TODO: need to 'push' the full-stack of the lineage object into the the line (object/variable) 
                    //      so that we've got the FULL / coordinated location of the file (e.g., if .build.sql does a file include, and that file include includes a file... and that's where we're at, the "line" needs to know it's position in the line AND ... that it has 2x parents so that errors can be passed out with some decent context.
                    
                    // TODO: either I need to evaluate tokens here - as I'm doing with BuildFile.ParseLines() or... BuildFile.ParseLines() shouldn't care about tokens either. 
                    //      and, arguably, I think I'm at the point where NEITHER this nor BuildFile.ParseLines should care about tokens. 
                    //          i think it'll have to be the 'job' of the BuildManifest to care about those and evaluate them. 
                    //          i.e., I'm no longer going to try and 'eager' evaluate them because ... of the recursive-allowed nature of INCLUDES. 

                    if (line.LineType.HasFlag(LineType.Directive))
                    {
                        if (line.Directive.DirectiveName == "FILE")
                        {
                            // TODO: nestedSourceFiles (and Directories) should handle file/path validation ... 
                            //  and if their paths aren't valid... 'throw' (lodge/store) a Parser or Build error (depending upon whether the directive is mal-formed/wrong (parser error) or ... whether the file isn't found (runtime/build error).
                            var nestedSourceFile = new IncludedFile((IncludeFileDirective)line.Directive);

                            lineage.AddSourceFile(nestedSourceFile.SourceFiles[0]);
                            var recursiveResult = LineParser.Instance.ParseLines(nestedSourceFile.SourceFiles[0], lineage);

                            if (recursiveResult.Errors.Count > 0)
                                buffer.Errors.AddRange(recursiveResult.Errors);

                            buffer.Lines.AddRange(recursiveResult.Lines);
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
                        buffer.Lines.Add(line);


                        // i.e., if this ISN'T a Directive Line... 
                        // if it's a comment-line... figure out what to do with comments based on: 
                        //      a) InlineDocumentation setting (i.e., the VERB - are we just building a .sql file, or are we building docs (or both)? 
                        //              i.e., if DOCS or BOTH then squirrel pertinent regex'd lines into some sort of global
                        //                  catalog - by file-name
                        //                  hmmm.. how the EFF do I get the object/sproc/udf/whatever name? 
                        //      b) comment-strip/remove directives... 
                        //          remove lines if comment-removal directive stipulates that we should... 
                        // and... it's entirely possible that I might need to skip/ignore the STRIP process (and even the comments-process above)
                    }
                }
            }

            var output = new SourceFileProcessingResult();
            if(buffer.Errors.Count > 0)
                output.Errors.AddRange(buffer.Errors);

            // TODO:
            //    

            bool processComments = false; 
            if (processComments)
            {
                foreach (var line in output.Lines)
                {
                    //if (line.IsComment)
                    //{
                    //    // if the comment is part of a header and/or is marked as belonging to inline/infile docs... 
                    //    //  and if we're handling docs... 
                    //    //      then DocsCatalog.AddSourceFileLine(line, sourceFile/lineageAndOtherMetaData);


                    //    // if fileRemovalPreferences.DictateThatWeShouldNOT_include_this_line
                    //    //  continue
                    //    // otherwise, drop out the bottom 
                    //}

                    output.Lines.Add(line);
                }
            }
            else 
                output.Lines.AddRange(buffer.Lines);

            return output;
        }
    }
}