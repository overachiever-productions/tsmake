namespace tsmake.workers;

// The REGIONS of T-SQL text - as SQL Server's lexer sees them.
public enum SqlRegion
{
    Code,
    EolComment,             // -- ... (to the end of the line)
    BlockComment,           // /* ... */ - which can be NESTED: /* outer /* inner */ still outer */
    String,                 // '...' (and N'...') - '' is an escaped tick
    BracketedIdentifier,    // [...] - ]] is an escaped ]
    QuotedIdentifier        // "..." - "" is an escaped " (a string - instead of an identifier - if QUOTED_IDENTIFIER is OFF; same rules either way)
}

// A minimal T-SQL lexer: works out which REGION each character is in - left-to-right, the same way SQL Server does - so that
//      each delimiter only 'counts' when it's in CODE: a tick in a -- comment, a /* in a string, a [ in a block comment, etc. are
//      all just content.
//  Works ONE LINE AT A TIME (each line WITHOUT its line break) - carrying its state from one line to the next - because strings,
//      block comments and identifiers can all span lines (while a -- comment always ends with its line).
//  Shared by the Normalizer (closure validation) and the TokenTransformer (token exclusions + escaping).
public sealed class TSqlScanner
{
    private int _commentDepth;

    // The region at the START of the next line to be scanned - i.e., whatever is still open from previous lines (never EolComment).
    public SqlRegion Region { get; private set; } = SqlRegion.Code;

    /// <summary>
    /// Scans one line (WITHOUT its line break).
    ///   regionStarts: called with (index, region) each time the region changes on this line - index is the FIRST character of the
    ///       new region: the ' that opens a String, the character just AFTER the ' that closes it (Code), etc.
    ///   strayCommentEnd: called with the index of each */ found in CODE (i.e., without a matching /*).
    /// </summary>
    public void ScanLine(string line, Action<int, SqlRegion>? regionStarts = null, Action<int>? strayCommentEnd = null)
    {
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            char next = i + 1 < line.Length ? line[i + 1] : '\0';

            switch (this.Region)
            {
                case SqlRegion.Code:
                    if (c == '-' && next == '-')
                    {
                        Enter(SqlRegion.EolComment, i);
                        i = line.Length;        // the rest of the line is comment.
                    }
                    else if (c == '/' && next == '*')
                    {
                        this._commentDepth = 1;
                        Enter(SqlRegion.BlockComment, i);
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        // NOT when the '/' starts a comment: "SELECT t.*/* all columns */" or "2*/* times */3" are valid.
                        bool slashStartsComment = i + 2 < line.Length && line[i + 2] == '*';
                        if (!slashStartsComment)
                        {
                            strayCommentEnd?.Invoke(i);
                            i++;
                        }
                    }
                    else if (c == '\'')
                        Enter(SqlRegion.String, i);
                    else if (c == '[')
                        Enter(SqlRegion.BracketedIdentifier, i);
                    else if (c == '"')
                        Enter(SqlRegion.QuotedIdentifier, i);
                    break;

                case SqlRegion.BlockComment:
                    if (c == '/' && next == '*')
                    {
                        this._commentDepth++;
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        this._commentDepth--;
                        i++;
                        if (this._commentDepth == 0)
                            Enter(SqlRegion.Code, i + 1);
                    }
                    break;

                case SqlRegion.EolComment:
                    break;

                default:    // String, BracketedIdentifier, QuotedIdentifier: closed by their delimiter - unless it's doubled (escaped).
                    char closer = this.Region switch
                    {
                        SqlRegion.String => '\'',
                        SqlRegion.BracketedIdentifier => ']',
                        _ => '"'
                    };

                    if (c == closer)
                    {
                        if (next == closer)
                            i++;
                        else
                            Enter(SqlRegion.Code, i + 1);
                    }
                    break;
            }
        }

        // A -- comment ends with its line; everything else (string, block comment, identifier) carries on to the next one.
        if (this.Region == SqlRegion.EolComment)
            this.Region = SqlRegion.Code;

        void Enter(SqlRegion region, int index)
        {
            this.Region = region;
            regionStarts?.Invoke(index, region);
        }
    }
}
