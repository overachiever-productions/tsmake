using tsmake.data_models;

namespace tsmake.workers;

public interface ITokenTransformer
{
    void TransformBuildArtifacts(List<IArtifact> artifacts, List<ISyntaxError> syntaxErrors);
    void TransformCodeLines(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors);
}

public class TokenTransformer(TokenExclusionDirectives tokenExclusionDirectives, ITokenDefinitionRegistry tokenDefinitionRegistry) : ITokenTransformer
{
    private TokenExclusionDirectives TokenExclusionDirectives { get; } = tokenExclusionDirectives;
    private ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    // Replaces {{##NAME}} tokens in the build's ARTIFACTS - i.e., the bits that come from ##OUTPUT and ##FILE-MARKER directives:
    //      - BuildArtifact (##OUTPUT):           its .Path.
    //      - FileMarkerArtifact (##FILE-MARKER): its .Path and its .ArtifactBody (the [body: ...] text - if there is one).
    //  Same token rule as code lines (see ReplaceTokens) - BUT these are PLAIN TEXT (file paths, markdown), NOT T-SQL: so there are no
    //      regions - i.e., -TokenExclusions don't apply and values are never escaped. (An apostrophe in a markdown body isn't a string.)
    //  UNLIKE code lines: a token WITHOUT a definition is a syntax error (and is left as-is). A file named {{##VERSION}}.marker.md -
    //      or a marker body that still says {{##SUMMARY}} - is never what anyone meant (e.g., -Version or -Summary wasn't specified).
    //  NOTE: paths are NOT validated here (e.g., a value with characters that aren't legal in a file name) - that's for whatever
    //      translates/validates the artifact's path AFTER its tokens are replaced.
    public void TransformBuildArtifacts(List<IArtifact> artifacts, List<ISyntaxError> syntaxErrors)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        if (artifacts.Count == 0)
            return;

        foreach (var artifact in artifacts)
        {
            switch (artifact.ArtifactType)
            {
                case ArtifactType.Build:
                    var build = (BuildArtifact)artifact;
                    build.SetPath(this.ReplaceArtifactTokens(build.Path, "the ##OUTPUT path", artifact, syntaxErrors));
                    break;
                case ArtifactType.FileMarker:
                    var marker = (FileMarkerArtifact)artifact;
                    marker.SetPath(this.ReplaceArtifactTokens(marker.Path, "the ##FILE-MARKER path", artifact, syntaxErrors));
                    if (marker.ArtifactBody is not null)
                        marker.SetBody(this.ReplaceArtifactTokens(marker.ArtifactBody, "the ##FILE-MARKER [body:]", artifact, syntaxErrors));
                    break;
                default:
                    throw new NotImplementedException($"Token replacement is not implemented for {artifact.ArtifactType} artifacts.");
            }
        }
    }

    // Plain-text replacement (no regions) + ONE syntax error per text that has undefined tokens - listing each distinct one.
    private string ReplaceArtifactTokens(string text, string description, IArtifact artifact, List<ISyntaxError> syntaxErrors)
    {
        if (!text.Contains("{{##", StringComparison.Ordinal))
            return text;

        var undefined = new List<string>();
        string transformed = this.ReplaceTokens(text, regionAt: null, onUndefined: token => { if (!undefined.Contains(token)) undefined.Add(token); }, out _);

        if (undefined.Count > 0)
        {
            string message = $"Undefined token{(undefined.Count > 1 ? "s" : "")} in {description}: {string.Join(", ", undefined)}. " +
                "Define each token (e.g., via -Version, -Summary or -Tokens) - or remove it.";

            // ##FILE-MARKER artifacts know their directive (and so, its line). A BuildArtifact doesn't (yet) - so: no line number.
            ICodeLine? codeLine = (artifact as FileMarkerArtifact)?.SourceDirective?.CodeLine;
            syntaxErrors.Add(codeLine is not null
                ? new SyntaxError(message, text, codeLine)
                : new SyntaxError(message, text, 0, new FileDetail($"({artifact.SourceType})", 0, 0)));
        }

        return transformed;
    }

    // Replaces {{##NAME}} tokens (see Global.TokenRegex and ReplaceTokens) with their defined values - in codeLine.TransformedContent:
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
    public void TransformCodeLines(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
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

            string transformed = this.ReplaceTokens(content, regionAt: index => RegionAt(regions, index), onUndefined: null, out bool replaced);

            if (replaced)
                codeLine.TransformedContent = transformed;
        }
    }

    // THE token rule - in ONE place (for code lines AND artifacts):
    //      each {{##NAME}} (see Global.TokenRegex) with a definition is swapped for its value. A token WITHOUT a definition is left
    //      as-is - and passed to onUndefined (if specified). Single pass: a value that itself contains a {{##TOKEN}} is NOT expanded.
    //  regionAt: for T-SQL (code lines) ONLY - the SqlRegion at a token's index: tokens in excluded regions are left as-is (and are
    //      NOT passed to onUndefined), and values are escaped for the region they land in (see Escape()).
    //      null: PLAIN TEXT (artifact paths, a marker's body) - no exclusions, no escaping.
    //  replaced: true if at least one token was replaced.
    private string ReplaceTokens(string text, Func<int, SqlRegion>? regionAt, Action<string>? onUndefined, out bool replaced)
    {
        bool any = false;
        string transformed = Global.TokenRegex.Replace(text, token =>
        {
            SqlRegion? region = regionAt?.Invoke(token.Index);
            if (region is { } r && this.IsExcluded(r))
                return token.Value;

            if (!this.TokenDefinitionRegistry.DefinedTokens.TryGetValue("##" + token.Groups["name"].Value, out var definition))
            {
                onUndefined?.Invoke(token.Value);
                return token.Value;
            }

            any = true;
            return region is { } sqlRegion ? Escape(definition.SpecifiedValue, sqlRegion) : definition.SpecifiedValue;
        });

        replaced = any;
        return transformed;
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