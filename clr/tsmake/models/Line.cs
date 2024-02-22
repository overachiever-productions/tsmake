using tsmake.models.directives;

namespace tsmake.models
{
    public class Line
    {
        public int LineNumber { get; }
        public string Content { get; }
        public string Source { get; }
        public LineType LineType { get; set; }
        public List<TokenInstance> Tokens { get;}
        //public List<IDirectiveInstance> Directives { get;}
        public IDirectiveInstance Directive { get; private set; }

        public Line(int number, string content, string source)
        {
            this.Tokens = new List<TokenInstance>();
            //this.Directives = new List<IDirectiveInstance>();

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

                    IDirectiveInstance instance = DirectiveFactory.CreateDirective(directiveName, this, location);
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

                        TokenInstance i = new TokenInstance(tokenData, location);
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
}