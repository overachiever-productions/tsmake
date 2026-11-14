namespace tsmake.data_models;

// Represents the definition of a TOKEN within PowerShell/etc. - i.e., via command line, objects, or .config file. 
public interface ITokenDefinition
{
    string Name { get; }
    string SpecifiedValue { get; set; }

    // https://overachieverllc.atlassian.net/browse/TSM-41
    //bool AllowBlanks { get; }
    //bool AllowInlineDefaults { get; }
    //bool AllowOverwrite { get; }
}

public class TokenDefinition(string name, string specifiedValue) : ITokenDefinition
{
    public string Name { get; } = "##" + name.Trim().ToUpperInvariant().Replace("##", "", StringComparison.OrdinalIgnoreCase);
    public string SpecifiedValue { get; set; } = specifiedValue.Trim();

    // https://overachieverllc.atlassian.net/browse/TSM-41
    //public bool AllowBlanks { get; }
    //public bool AllowInlineDefaults { get; }
    //public bool AllowOverwrite { get; }

    //public TokenDefinition(string name, string specifiedValue) : this(name, specifiedValue, "") { }

    //public TokenDefinition(string name, string specifiedValue, bool allowInlineDefaults = false, bool allowBlanks = false)
    //{
    //    this.Name = name;
    //    this.SpecifiedValue = specifiedValue;
    //    //this.AllowInlineDefaults = allowInlineDefaults;
    //    //this.AllowBlanks = allowBlanks;
    //}
}

public interface ITokenDefinitionRegistry
{
    Dictionary<string, TokenDefinition> DefinedTokens { get; }
    TokenDefinition GetTokenDefinition(string tokenName);
    //void SetToken(TokenDefinition token, bool allowOverwrite, string source);
    
    void SetToken(TokenDefinition token);
    void RemoveTokens();
}

public class TokenDefinitionRegistry : ITokenDefinitionRegistry
{
    public Dictionary<string, TokenDefinition> DefinedTokens { get; private set; }

    public TokenDefinition GetTokenDefinition(string tokenName)
    {
        if (this.DefinedTokens.TryGetValue(tokenName, out var definition))
            return definition;

        return null!;
    }

    // https://overachieverllc.atlassian.net/browse/TSM-41
    //public static TokenDefinitionRegistry Instance => new TokenDefinitionRegistry();

    public static TokenDefinitionRegistry NewRegistry => new TokenDefinitionRegistry();

    public static TokenDefinitionRegistry CreateRegistry(IEnumerable<ITokenDefinition> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var registry = new TokenDefinitionRegistry();
        foreach (var token in tokens)
        {
            ArgumentNullException.ThrowIfNull(token);
            registry.SetToken(token as TokenDefinition ?? new TokenDefinition(token.Name, token.SpecifiedValue));
        }

        return registry;
    }

    private TokenDefinitionRegistry()
    {
        this.DefinedTokens = new Dictionary<string, TokenDefinition>();
    }

    // https://overachieverllc.atlassian.net/browse/TSM-41
    //public void SetToken(TokenDefinition token, bool allowOverwrite, string source)
    //{
    //    if (this.DefinedTokens.ContainsKey(token.Name))
    //    {
    //        if (allowOverwrite)
    //        {
    //            var target = this.DefinedTokens[token.Name];
    //            target.SpecifiedValue = token.SpecifiedValue;
    //            return;
    //        }

    //        throw new Exception("CodeFileToken already exists... i.e., is a duplicate");
    //    }

    //    this.DefinedTokens.Add(token.Name, token);
    //}

    public void SetToken(TokenDefinition token)
    {
        if (this.DefinedTokens.ContainsKey(token.Name))
        {
            var target = this.DefinedTokens[token.Name];
            target.SpecifiedValue = token.SpecifiedValue;
            return;
        }
        this.DefinedTokens.Add(token.Name, token);
    }

    public void RemoveTokens()
    {
        this.DefinedTokens = new Dictionary<string, TokenDefinition>();
    }
}

// Represents a CodeFileToken within a .SQL file - that needs to be replaced/etc. 
public class CodeFileToken(string tokenValue, ICodeLine codeLine)
{
    public string Name { get; } = tokenValue.ToUpperInvariant();
    public ICodeLine CodeLine { get; } = codeLine;

    // https://overachieverllc.atlassian.net/browse/TSM-40
    //public string DefaultValue { get; }

    //public CodeFileToken(string tokenValue, string defaultValue)
    //{
    //    DefaultValue = defaultValue;
    //    if (tokenValue.Contains(":", StringComparison.InvariantCultureIgnoreCase))
    //    {
    //        var parts = tokenValue.Split(':', StringSplitOptions.None);
    //        this.Name = parts[0].ToUpperInvariant();
    //        this.DefaultValue = tokenValue.Substring((this.Name.Length) + 1);
    //    }
    //    else
    //        this.Name = tokenValue.ToUpperInvariant();
    //}


}