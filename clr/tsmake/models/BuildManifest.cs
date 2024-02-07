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

                    if(line.Directives.Count > 0)
                        this.Directives.AddRange(line.Directives);

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