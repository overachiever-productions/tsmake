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

public class TokenDefinition : ITokenDefinition
{
    // Token NAMES: letters and numbers - with single dashes BETWEEN them (e.g., PROJECT-SITE) - and an optional ## prefix.
    //      Case doesn't matter here (names are stored UPPER-CASE: "##PROJECT-SITE") - but tokens in .sql files must be upper-case.
    //      Same rules as Global.TokenRegex - so that a defined token can always be matched.
    private static readonly Regex ValidName = new(@"\A(?:\#\#)?[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*\z", RegexOptions.CultureInvariant);

    private string _specifiedValue = string.Empty;

    // NOTE: VALIDATES name + value (throws ArgumentException) - so every TokenDefinition is valid, however it was created
    //      (PowerShell, CreateRegistry(), etc.). Callers that want to 'fail early' (with their own context) can use GetValidationError().
    public TokenDefinition(string name, string specifiedValue)
    {
        string? error = GetValidationError(name, specifiedValue);
        if (error != null)
            throw new ArgumentException(error);

        string trimmed = name.Trim();
        if (trimmed.StartsWith("##", StringComparison.Ordinal))
            trimmed = trimmed.Substring(2);

        this.Name = "##" + trimmed.ToUpperInvariant();
        this.SpecifiedValue = specifiedValue;
    }

    public string Name { get; }

    // Trimmed. Can be empty (a token can be replaced with nothing) - but can NOT contain line breaks.
    public string SpecifiedValue
    {
        get => this._specifiedValue;
        set
        {
            string? error = GetValueError(this.Name, value);
            if (error != null)
                throw new ArgumentException(error, nameof(value));

            this._specifiedValue = value.Trim();
        }
    }

    // null if name + value make a valid token definition - otherwise, the problem.
    public static string? GetValidationError(string? name, string? value)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return "A token name is required.";

        if (!ValidName.IsMatch(trimmed))
            return $"Invalid token name '{trimmed}': names may only contain letters (A-Z) and numbers (0-9) - with single dashes between them (e.g., ##PROJECT-SITE) - and an optional ## prefix.";

        return GetValueError(trimmed, value);
    }

    private static string? GetValueError(string name, string? value)
    {
        if (value is null)
            return $"Token '{name}' needs a value (use an empty string for a blank value).";

        if (value.IndexOfAny(['\r', '\n']) >= 0)
            return $"Token '{name}' has an invalid value: values can't contain line breaks (CR or LF).";

        return null;
    }

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

        // NOTE: names + values are VALIDATED here too (via the TokenDefinition constructor - and SpecifiedValue's setter) - i.e., we
        //      do NOT assume that callers (PowerShell, or anyone else) have already validated them. Invalid => ArgumentException.
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