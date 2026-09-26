namespace tsmake.data_models;

public interface IRankedString
{
    SourceType SourceType { get; }
    string Value { get; }
}

public class RankedString(SourceType sourceType, string value) : IRankedString
{
    public SourceType SourceType { get; set; } = sourceType;
    public string Value { get; set; } = value;

    public static string GetRankedValue(List<IRankedString> rankedStrings)
    {
        if (rankedStrings.Count == 0)
            return string.Empty;

        return rankedStrings
            .OrderBy(rs => rs.SourceType.Priority())
            .FirstOrDefault()?.Value ?? string.Empty;
    }
}