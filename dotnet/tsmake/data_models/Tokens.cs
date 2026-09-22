namespace tsmake.data_models;

// Represents the definition of a TOKEN within PowerShell/etc. - i.e., via command line, objects, or .config file. 
public interface ITokenDefinition
{
    string Name { get; }
    string DefaultBuildValue { get; } // what we get from a CONFIG ... or from the default-part of the serialization ... 
    string SpecifiedBuildValue { get; set; } // override of the default IF something is explicitly set via CONFIG or command line. i.e., each token can be SPECIFIED with a name:default (in a FILE) ... config might overwrite the default. or user/command-line might overwrite. 
    bool AllowBlanks { get; }
    bool AllowInlineDefaults { get; }
}

public class TokenDefinition : ITokenDefinition
{
    public string Name { get; }
    public string DefaultBuildValue { get; }    // what we get from a CONFIG ... or from the default-part of the serialization ... or within a .SQL build file... 
    public string SpecifiedBuildValue { get; set; }  // override of the default IF something is explicitly set via CONFIG or command line. i.e., each token can be SPECIFIED with a name:default (in a FILE) ... config might overwrite the default. or user/command-line might overwrite. 
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

public interface ITokenDefinitionRegistry
{
    Dictionary<string, TokenDefinition> DefinedTokens { get; }
    TokenDefinition GetTokenDefinition(string tokenName);
    void SetToken(TokenDefinition token, bool allowOverwrite, string source);
    void RemoveTokens();
}

public class TokenDefinitionRegistry : ITokenDefinitionRegistry
{
    public Dictionary<string, TokenDefinition> DefinedTokens { get; private set; }

    public TokenDefinition GetTokenDefinition(string tokenName)
    {
        if (this.DefinedTokens.ContainsKey(tokenName))
            return this.DefinedTokens[tokenName];

        return null!;
    }

    public static TokenDefinitionRegistry Instance => new TokenDefinitionRegistry();

    private TokenDefinitionRegistry()
    {
        this.DefinedTokens = new Dictionary<string, TokenDefinition>();
    }

    public void SetToken(TokenDefinition token, bool allowOverwrite, string source)
    {
        if (this.DefinedTokens.ContainsKey(token.Name))
        {
            if (allowOverwrite)
            {
                var target = this.DefinedTokens[token.Name];
                target.SpecifiedBuildValue = token.SpecifiedBuildValue;
                return;
            }

            throw new Exception("Token already exists... i.e., is a duplicate"); // TODO: explain allowOverwrite ... 
        }

        this.DefinedTokens.Add(token.Name, token);
    }

    public void RemoveTokens()
    {
        this.DefinedTokens = new Dictionary<string, TokenDefinition>();
    }
}

// Represents a Token within a .SQL file - that needs to be replaced/etc. 
public class Token
{
    public string Name { get; }
    public string DefaultValue { get; }

    //public Stack<Location> Location { get; private set; }

    public Token(string tokenValue, string defaultValue)
    {
        DefaultValue = defaultValue;
        if (tokenValue.Contains(":", StringComparison.InvariantCultureIgnoreCase))
        {
            var parts = tokenValue.Split(':', StringSplitOptions.None);
            this.Name = parts[0].ToUpperInvariant();
            this.DefaultValue = tokenValue.Substring((this.Name.Length) + 1);
        }
        else
            this.Name = tokenValue.ToUpperInvariant();
    }

    //public void SetLocation(Stack<Location> location, int startPosition)
    //{
    //    var clone = location.DeepClone();
    //    if (clone.Peek().Column != startPosition)
    //    {
    //        Location old = clone.Pop();
    //        old.Column = startPosition;
    //        clone.Push(old);
    //    }

    //    this.Location = clone;
    //}

    //public override bool Equals(object other)
    //{
    //    if (other == null)
    //        return false;

    //    var otherToken = (Token)other;
    //    if (otherToken.ToString() == this.ToString())
    //        return true;

    //    return false;
    //}

    //public override int GetHashCode()
    //{
    //    return this.ToString().GetHashCode();
    //}

    //public override string ToString()
    //{
    //    var defaultValue = "<EMPTY>";
    //    if (this.DefaultValue != null)
    //        defaultValue = this.DefaultValue;

    //    return $"NAME:\"{this.Name}\";DEFAULT:\"{defaultValue}\" => {this.Location.Peek().FileName}({this.Location.Peek().ParentLineNumer},{this.Location.Peek().Column})";
    //}
}