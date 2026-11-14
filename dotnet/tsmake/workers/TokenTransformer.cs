using tsmake.data_models;

namespace tsmake.workers;

public interface ITokenTransformer
{
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34
    void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors);
}

public class TokenTransformer(TokenExclusionDirectives tokenExclusionDirectives, ITokenDefinitionRegistry tokenDefinitionRegistry) : ITokenTransformer
{
    private TokenExclusionDirectives TokenExclusionDirectives { get; } = tokenExclusionDirectives;
    private ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34

    // Replaces {{##NAME}} tokens (see Global.TokenRegex) with their defined values - in codeLine.TransformedContent:
    //  1. codeLines are the ARTIFACT's lines - in output order (i.e., included files' lines inline) - and are scanned as ONE text
    //      with the (shared) TSqlScanner - so each token's REGION (code, string, comment, identifier) is what SQL Server will see
    //      in the final artifact. (NOTE: a file with an unclosed string/comment - already a Normalizer syntax error - 'swallows'
    //      every line after it here, too.)
    //  2. A token in a region excluded by this.TokenExclusionDirectives (strings, -- comments, /* block comments */ - any
    //      combination: they're flags) is left as-is. Tokens in code, [identifiers] and "identifiers" are always replaced.
    //  3. Tokens WITHOUT a definition are (for now) left exactly as they are: no errors, no change.
    //  4. Values are swapped in as-is (empty values included) - except INSIDE a string or identifier, where the region's closing
    //      delimiter is doubled so the value can't end it early:  ' => ''   ] => ]]   " => ""
    //  5. Single pass: a value that itself contains a {{##TOKEN}} is NOT expanded. (Values can't contain line breaks - see TokenDefinition.)
    public void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
    {
        if (codeLines.Count == 0 || this.TokenDefinitionRegistry.DefinedTokens.Count == 0)
            return;     // nothing can be replaced - and undefined tokens are left as-is.

        var scanner = new TSqlScanner();
        var regions = new List<(int Start, SqlRegion Region)>();

        foreach (var codeLine in codeLines)
        {
            string content = codeLine.GetContent();

            if (!content.Contains("{{##", StringComparison.Ordinal))
            {
                scanner.ScanLine(content);      // no tokens - but still needed: strings/comments can span lines.
                continue;
            }

            regions.Clear();
            regions.Add((0, scanner.Region));
            scanner.ScanLine(content, regionStarts: (index, region) => regions.Add((index, region)));

            bool replaced = false;
            string transformed = Global.TokenRegex.Replace(content, token =>
            {
                SqlRegion region = RegionAt(regions, token.Index);
                if (this.IsExcluded(region))
                    return token.Value;

                if (!this.TokenDefinitionRegistry.DefinedTokens.TryGetValue("##" + token.Groups["name"].Value, out var definition))
                    return token.Value;

                replaced = true;
                return Escape(definition.SpecifiedValue, region);
            });

            if (replaced)
                codeLine.TransformedContent = transformed;
        }
    }

    private bool IsExcluded(SqlRegion region) => region switch
    {
        SqlRegion.String => this.TokenExclusionDirectives.HasFlag(TokenExclusionDirectives.ExcludeStrings),
        SqlRegion.EolComment => this.TokenExclusionDirectives.HasFlag(TokenExclusionDirectives.ExcludeEolComments),
        SqlRegion.BlockComment => this.TokenExclusionDirectives.HasFlag(TokenExclusionDirectives.ExcludeBlockComments),
        _ => false
    };

    private static string Escape(string value, SqlRegion region) => region switch
    {
        SqlRegion.String => value.Replace("'", "''"),
        SqlRegion.BracketedIdentifier => value.Replace("]", "]]"),
        SqlRegion.QuotedIdentifier => value.Replace("\"", "\"\""),
        _ => value
    };

    // regions: (start, region) pairs in line order - so the region at index is the LAST one starting at (or before) it.
    private static SqlRegion RegionAt(List<(int Start, SqlRegion Region)> regions, int index)
    {
        for (int i = regions.Count - 1; i > 0; i--)
        {
            if (regions[i].Start <= index)
                return regions[i].Region;
        }

        return regions[0].Region;
    }
}