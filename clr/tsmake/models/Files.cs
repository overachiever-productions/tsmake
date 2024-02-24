namespace tsmake.models
{

    // REFACTOR: may want to a) rename this to 'BuildFile' and b) move it into a folder called 'Files' - along with CodeFile (for an 'include') and such...
    //      that way I'd have: 
    //          - a BuildFile to represent ... the build file. 
    //          - CodeFile(s) to represent - code files that are included via DIRECTORY or FILE 
    //                  and might even make sense to have a DirectoryManifest
    //          - a BuildMANIFEST would then be the intermediate stage - i.e., once we'd processed/shredded the BuildFile and included ALL directives... I'd have
    //                  a Build 'Manifest'... 
    //                  where the only things left to be processed would be CONDITIONAL directives and Tokens. 
    //          - finally, a BufferManifest or OutputFile (i.e., 2 different NAMING options for the same things) would be the final 'processor' - that'd be dumped/written to a flat-file/artifact. 
    //              
    public class BuildFile
    {
        // TODO: I don't think the source needs to be a) public, b) a property.  - i.e., I can make it a field instead. 
        public string Source { get; }
        public List<ParserError> NonFatalParserErrors { get; }      // TODO: I'm not sure these'll ever even be used/needed. i.e., UNLESS there's some sort of 'warning' or whatever that can happen down within .ParseLines, then this isn't needed. 
        public List<ParserError> FatalParserErrors { get; }
        public List<Line> Lines { get; }
        public List<TokenInstance> Tokens { get; }
        public List<IDirective> Directives { get; }

        public RootPathDirective RootDirective { get; private set; }
        public OutputDirective OutputDirective { get; private set; }

        public BuildFile(string buildFile)
        {
            this.Source = buildFile;
            this.NonFatalParserErrors = new List<ParserError>();
            this.FatalParserErrors = new List<ParserError>();
            this.Lines = new List<Line>();
            this.Tokens = new List<TokenInstance>();
            this.Directives = new List<IDirective>();

            this.ParseLines();
        }

        private void ParseLines()
        {
            string current;
            int i = 1;  // Doh. These are line numbers - and need to start at 1 - not 0. 
            using (var buildFile = new StreamReader(this.Source))
            {
                while ((current = buildFile.ReadLine()) != null)
                {
                    var line = new Line(i, current, this.Source);

                    if (line.Tokens.Count > 0)
                        this.Tokens.AddRange(line.Tokens);

                    if (line.LineType.HasFlag(LineType.Directive))
                    {
                        // TODO: there are a few directives we can't duplicate - like: ROOT, OUTPUT, VERSION-CHECKER, etc. 
                        if (line.Directive.DirectiveName == "ROOT")
                        {
                            if (null == this.RootDirective)
                                this.RootDirective = (RootPathDirective)line.Directive;
                            else
                            {
                                string errorMessage = $"Duplicate ROOT: directive detected in file: [{line.Directive.Line.Source}].";
                                string context = $"First ROOT: defined on line: [{this.RootDirective.Location.LineNumber}].";
                                context += Environment.NewLine;
                                context += "Duplicate ROOT: defined on line: [{line.Directive.Location.LineNumber}].";

                                var parserError = new ParserError(ErrorSeverity.Fatal, errorMessage, line.Directive.Location,  context);
                                this.FatalParserErrors.Add(parserError);
                                continue; // don't add to .Directives - just move on to the next directive, etc. 
                            }
                        }

                        if (line.Directive.DirectiveName == "OUTPUT")
                        {
                            if (null == this.OutputDirective)
                                this.OutputDirective = (OutputDirective)line.Directive;
                            else
                            {
                                string errorMessage = $"Duplicate OUTPUT: directive detected in file: [{line.Directive.Line.Source}].";
                                string context = $"First OUTPUT: defined on line: [{this.OutputDirective.Location.LineNumber}].";
                                context += Environment.NewLine;
                                context += "Duplicate OUTPUT: defined on line: [{line.Directive.Location.LineNumber}].";

                                var parserError = new ParserError(ErrorSeverity.Fatal, errorMessage, line.Directive.Location, context);
                                this.FatalParserErrors.Add(parserError);
                                continue;
                            }
                        }

                        this.Directives.Add(line.Directive);
                    }

                    // NOTE: in terms of comments, there are a few different types: 
                    //      - tsmake comment (i.e, directive).
                    //      - line-terminating comment - e.g., -- xxxx 
                    //          and... this can be at the start of the line (i.e., whole line) or ... part of the way through the line. 
                    //          so... i'm probably going to want to differentiate between 'SingleLineComment' and 'SingleLineCommentThatIsTheWholeLine' ... 
                    //          though, obviously, i need better names... 
                    //      - multi-line comments (style)... which can be of ... multiple types: 
                    //          - inline - i.e. somewhere within a line is an /* ENTIRE SET OF COMMENTS */, but they're not the entire line. 
                    //          - starts i.e., /* and then some text (or not) and ... a carriage return. 
                    //          - midline... i.e., there was a /* somewhere and we haven't yet hit the line with */
                    //          - terminating ... 
                    //              and... terminating with text/whitespace after it OR terminating with NOTHING after it?


                    // TODO: if ... line.CommentType == CommentType.MultilineStart
                    //      then... mark that ... somehow that we're 'in' a multi-line comment... 
                    //          only... in order to do that, I think I need to pass in that ... the line started at 'i' and is still going... 
                    //          as in... i'd have to push it into the .ctor or the Line() itself so'z I could track the STATE (i.e., we're in a multi-line comment)
                    //          and that ... it started on (previous)i and.... then, I'd have to do something along the lines of passing in the 'end' of the multi-line comment too.

                    this.Lines.Add(line);
                    i++;
                }
            }
        }
    }

    public class CodeFile
    {
        // represents an individual 'include' (i.e., the guts/contents of a .sql file that is added to the build). 
        //      can be a .FILE include, a .DIRECTORY (file) include, or the recursive instance of a file in EITHER of those. 

        // NOTE: BuildFile's .ParseLines is ... going to need to be something that's pushed out into a 'helper' or 
        //  whatever so that ... yup, it can be included here. 

    }

    // and. of course... the other problem with 'Directory' is that it's already part of System.IO
    //public class Directory
    //{
    //    // might not want to have this... but, might. 
    //    //      if i do it's the representation of every file in a DIRECTORY (include) - along with recursive ... includes and so on. 
    //}
}