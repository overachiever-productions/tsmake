namespace tsmake.models
{
    public class TokenDefinition
    {
        public string Name { get; }
        public string DefaultBuildValue { get; }
        public string SpecifiedBuildValue { get; set; }
        public bool AllowBlanks { get; }
        public bool AllowInlineDefaults { get; }

        public TokenDefinition(string name, string specifiedValue) : this(name, specifiedValue, "") { }

        public TokenDefinition(string name, string specifiedBuildValue, string defaultBuildValue, bool allowInlineDefaults = false, bool allowBlanks = false) 
        {
            this.Name = name;
            this.DefaultBuildValue = defaultBuildValue;
            this.SpecifiedBuildValue = specifiedBuildValue;
            this.AllowInlineDefaults = allowInlineDefaults;
            this.AllowBlanks = allowBlanks;
        }
    }

    public class Token
    {
        public string Name { get; }
        public string DefaultValue { get; }

        // TODO: this needs to be a Stack<Location>
        public Location Location { get; }

        public Token(string tokenValue, Location location)
        {
            if (tokenValue.Contains(":", StringComparison.InvariantCultureIgnoreCase))
            {
                var parts = tokenValue.Split(':', StringSplitOptions.None);
                this.Name = parts[0].ToUpperInvariant();
                this.DefaultValue = tokenValue.Substring((this.Name.Length) + 1);
            }
            else
            {
                this.Name = tokenValue.ToUpperInvariant();
            }

            this.Location = location;
        }

        public override bool Equals(object other)
        {
            if (other == null) return false;

            var otherToken = (Token)other;
            if (otherToken.ToString() == this.ToString())
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            var defaultValue = "<EMPTY>";
            if(this.DefaultValue != null)
                defaultValue = this.DefaultValue;

            return $"NAME:\"{this.Name}\";DEFAULT:\"{defaultValue}\" => {this.Location.FileName}({this.Location.LineNumber},{this.Location.Column})";
        }
    }

    public class TokenRegistry
    {
        public Dictionary<string, TokenDefinition> DefinedTokens { get; private set; }

        public TokenDefinition GetTokenDefinition(string tokenName)
        {
            if(this.DefinedTokens.ContainsKey(tokenName))
                return this.DefinedTokens[tokenName];

            return null;
        }

        public static TokenRegistry Instance => new TokenRegistry();

        private TokenRegistry()
        {
            this.DefinedTokens = new Dictionary<string, TokenDefinition>();
        }

        public void SetToken(TokenDefinition token, bool allowOverride)
        {
            if (this.DefinedTokens.ContainsKey(token.Name))
            {
                if (allowOverride)
                {
                    var target = this.DefinedTokens[token.Name];
                    target.SpecifiedBuildValue = token.SpecifiedBuildValue;
                    return;
                }

                throw new Exception("Token already exists... i.e., is a duplicate");
            }

            this.DefinedTokens.Add(token.Name, token);
        }

        public void RemoveTokens()
        {
            this.DefinedTokens = new Dictionary<string, TokenDefinition>();
        }
    }
}