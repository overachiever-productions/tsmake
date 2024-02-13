using System.IO;
using System.Collections.Generic;
using tsmake.models.directives;

namespace tsmake.models
{
    public class BuildManifest
    {
        public string Source { get; }
        public List<Line> Lines { get; }
        public List<TokenInstance> Tokens { get; }
        public List<IDirectiveInstance> Directives { get; }

        public BuildManifest(string buildFile)
        {
            this.Source = buildFile;
            this.Lines = new List<Line>();
            this.Tokens = new List<TokenInstance>();
            this.Directives = new List<IDirectiveInstance>();

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

                    if(line.LineType.HasFlag(LineType.Directive))
                        this.Directives.Add(line.Directive);

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
}