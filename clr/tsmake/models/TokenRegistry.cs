namespace tsmake.models
{
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