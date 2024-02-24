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

    public class TokenInstance
    {
        public string Name { get; }
        public string DefaultValue { get; }
        public Location Location { get; }

        public TokenInstance(string tokenValue, Location location)
        {
            if (tokenValue.Contains(":"))
            {
                var parts = tokenValue.Split(':');
                this.Name = parts[0].ToUpperInvariant();
                this.DefaultValue = parts[1];
            }
            else
            {
                this.Name = tokenValue.ToUpperInvariant();
            }

            this.Location = location;
        }
    }

    public class TokenRegistry
    {
        public Dictionary<string, TokenDefinition> DefinedTokens { get; private set; }

        public TokenDefinition GetTokenDefinition(string tokenName)
        {
            return this.DefinedTokens[tokenName];
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