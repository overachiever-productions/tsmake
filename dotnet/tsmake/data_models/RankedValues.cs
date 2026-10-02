namespace tsmake.data_models;

public interface IRankedString
{
    SourceType SourceType { get; }
    string Value { get; }
    IDirective SourceDirective { get; }
}

// WARNING: This class is defined as a single record type ... but the true 'logic'/power of ranked<anything> comes from being in a List<>
//      via the static GetRankedValue() method. TRANSLATION: there's NOTHING preventing me from being dumb and adding 2x .CommandLine inputs... 
//      a BETTER class (e.g., StringRanking - with it's OWN List<>) could prevent such an issue and/or duplicates, etc. 
public class RankedString(SourceType sourceType, string value) : IRankedString
{
    public SourceType SourceType { get; set; } = sourceType;
    public string Value { get; set; } = value;
    public IDirective SourceDirective { get; set; } = null!;

    // NOTE: C# will likely ONLY use this .ctor. But PowerShell may end up using the 2 parameter .ctor in a number of cases. 
    public RankedString(SourceType sourceType, string value, IDirective sourceDirective) : this(sourceType, value)
    {
        this.SourceDirective = sourceDirective;
    }

    public static IRankedString GetRankedValue(List<IRankedString> rankedStrings)
    {
        if (rankedStrings.Count == 0)
            return EmptyRankedString();

        return rankedStrings
            .Where(rs => !string.IsNullOrEmpty(rs.Value))
            .OrderBy(rs => rs.SourceType.Priority())    // .Priority() is an extension method
            .FirstOrDefault() ?? EmptyRankedString();
    }

    public static IRankedString EmptyRankedString()
    {
        return new RankedString(SourceType.None, string.Empty);
    }
}