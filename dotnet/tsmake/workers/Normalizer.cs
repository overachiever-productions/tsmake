using tsmake.data_models;

namespace tsmake.workers;

public interface INormalizer
{
    void Normalize(string fileContent, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, IFileDetail fileDetail);
    string NormalizedText { get; }
}

public class Normalizer(LineEndingsType lineEndingsType = LineEndingsType.CrLf) : INormalizer
{
    private string _input = string.Empty;
    private readonly LineEndingsType _lineEndingsType = lineEndingsType;

    public string NormalizedText { get; private set; } = string.Empty;

    public void Normalize(string fileContent, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, IFileDetail fileDetail)
    {
        this._input = fileContent;

        var regex = new Regex(@"\r\n|\r|\n", Global.SingleLineRegexOptions);
        var matches = regex.Matches(fileContent);

        int lineNumber = 1;
        int previousStart = 0;

        if (matches.Count < 1)
        {
            codeLines.Add(new CodeLine(fileContent, 1, 1, fileContent.Length, fileDetail));
        }
        else
        {
            foreach (Match match in matches)
            {
                int lineStart = previousStart;
                int lineEnd = match.Index;
                string lineText = this._input.Substring(lineStart, lineEnd - lineStart);

                var oneBasedLineStart = lineStart + 1;
                if (lineText.Equals(match.Value, StringComparison.InvariantCultureIgnoreCase))
                    oneBasedLineStart = lineStart;

                var oneBasedLineEnd = lineEnd;
                if (oneBasedLineEnd < lineStart) oneBasedLineEnd = lineStart;

                if (oneBasedLineEnd < oneBasedLineStart) oneBasedLineEnd = oneBasedLineStart;

                CodeLine line = new CodeLine(lineText, lineNumber, oneBasedLineStart, oneBasedLineEnd, fileDetail);
                codeLines.Add(line);

                previousStart = lineEnd + match.Length;
                lineNumber++;
            }

            var finalMatch = matches[^1];
            if (finalMatch.Index + finalMatch.Length == this._input.Length) // non-obvious logic here, but it's _NEEDED_ for trailing "empty lines".
                codeLines.Add(new CodeLine("", lineNumber, previousStart, previousStart, fileDetail));

            if (previousStart < this._input.Length)
                codeLines.Add(new CodeLine(this._input.Substring(previousStart, this._input.Length - previousStart), lineNumber, previousStart + 1, this._input.Length, fileDetail));
        }

        this.NormalizedText = this.NormalizeLineEndings();
        this.ValidateClosures(syntaxErrors, fileDetail);
    }

    private string NormalizeLineEndings()
    {
        var lineEnding = this._lineEndingsType switch
        {
            LineEndingsType.CrLf => "\r\n",
            LineEndingsType.Lf => "\n",
            LineEndingsType.Cr => "\r",
            _ => throw new InvalidEnumArgumentException()
        };

        return Regex.Replace(this._input, @"\r\n|\r|\n", lineEnding, Global.SingleLineRegexOptions);
    }

    private enum ScanState
    {
        Code,
        EolComment,             // -- ... (to the end of the line)
        BlockComment,           // /* ... */ - which can be NESTED: /* outer /* inner */ still outer */
        String,                 // '...' (and N'...') - '' is an escaped tick
        BracketedIdentifier,    // [...] - ]] is an escaped ]
        QuotedIdentifier        // "..." - "" is an escaped " (a string - instead of an identifier - if QUOTED_IDENTIFIER is OFF; same rules either way)
    }

    // Scans the ORIGINAL input left-to-right - the same way SQL Server's lexer does - so that each delimiter is only
    //      'seen' when it's in CODE: a tick in a -- comment, a /* in a string, a [ in a block comment, etc. are all just content.
    //  Reports:
    //      - every */ found in CODE (i.e., without a matching /*).
    //      - at most ONE non-terminated string/comment/identifier: whatever is still open at the end of the file - which,
    //        like SQL Server, swallows everything after it (so a tick within an unclosed block comment is NOT a 2nd error).
    //  Line numbers are counted while scanning (\r\n, \r and \n each end a line - same as Normalize()) - so they always
    //      match the CodeLines; the line-ending normalization (and any difference in lengths it causes) plays no part.
    private void ValidateClosures(List<ISyntaxError> syntaxErrors, IFileDetail fileDetail)
    {
        string text = this._input;
        if (string.IsNullOrEmpty(text))
            return;

        var state = ScanState.Code;
        int commentDepth = 0;
        int line = 1;
        int openedAt = -1;
        int openedOnLine = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (c is '\r' or '\n')
            {
                if (c == '\r' && next == '\n')
                    i++;

                line++;
                if (state == ScanState.EolComment)
                    state = ScanState.Code;

                continue;       // in any other state, a line break is just part of the string/comment/identifier.
            }

            switch (state)
            {
                case ScanState.Code:
                    if (c == '-' && next == '-')
                    {
                        state = ScanState.EolComment;
                        i++;
                    }
                    else if (c == '/' && next == '*')
                    {
                        state = ScanState.BlockComment;
                        commentDepth = 1;
                        (openedAt, openedOnLine) = (i, line);
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        // NOT when the '/' starts a comment: "SELECT t.*/* all columns */" or "2*/* times */3" are valid.
                        bool slashStartsComment = i + 2 < text.Length && text[i + 2] == '*';
                        if (!slashStartsComment)
                        {
                            syntaxErrors.Add(new SyntaxError("Unexpected End of Block-Comment (*/ without a matching /*).", RestOfLine(text, i), line, fileDetail));
                            i++;
                        }
                    }
                    else if (c is '\'' or '[' or '"')
                    {
                        state = c switch
                        {
                            '\'' => ScanState.String,
                            '[' => ScanState.BracketedIdentifier,
                            _ => ScanState.QuotedIdentifier
                        };
                        (openedAt, openedOnLine) = (i, line);
                    }
                    break;

                case ScanState.EolComment:
                    break;

                case ScanState.BlockComment:
                    if (c == '/' && next == '*')
                    {
                        commentDepth++;
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        commentDepth--;
                        i++;
                        if (commentDepth == 0)
                            state = ScanState.Code;
                    }
                    break;

                default:    // String, BracketedIdentifier, QuotedIdentifier: closed by their delimiter - unless it's doubled (escaped).
                    char closer = state switch
                    {
                        ScanState.String => '\'',
                        ScanState.BracketedIdentifier => ']',
                        _ => '"'
                    };

                    if (c == closer)
                    {
                        if (next == closer)
                            i++;
                        else
                            state = ScanState.Code;
                    }
                    break;
            }
        }

        string? nonTerminated = state switch
        {
            ScanState.String => "Non-Terminated String.",
            ScanState.BlockComment => "Non-Terminated Block-Comment.",
            ScanState.BracketedIdentifier => "Non-Terminated [Identifier].",
            ScanState.QuotedIdentifier => "Non-Terminated \"Identifier\".",
            _ => null      // Code or EolComment: nothing left open.
        };

        if (nonTerminated != null)
            syntaxErrors.Add(new SyntaxError(nonTerminated, RestOfLine(text, openedAt), openedOnLine, fileDetail));
    }

    // Error detail: from the problem to the end of ITS line - not the rest of the file, and without the line break.
    private static string RestOfLine(string text, int start)
    {
        int end = text.IndexOfAny(['\r', '\n'], start);
        return end < 0 ? text.Substring(start) : text.Substring(start, end - start);
    }
}