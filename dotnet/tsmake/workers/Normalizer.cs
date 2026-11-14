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
        int firstNewLine = codeLines.Count;     // codeLines MAY already contain other lines: only THIS file's are validated (below).

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
        ValidateClosures(codeLines.GetRange(firstNewLine, codeLines.Count - firstNewLine), syntaxErrors, fileDetail);
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

    // Scans the file's lines with the (shared) TSqlScanner - see TSqlScanner for the lexing rules. Reports:
    //      - every */ found in CODE (i.e., without a matching /*).
    //      - at most ONE non-terminated string/comment/identifier: whatever is still open at the end of the file - which,
    //        like SQL Server, swallows everything after it (so a tick within an unclosed block comment is NOT a 2nd error).
    //  Line numbers come straight from the CodeLines - so the line-ending normalization plays no part.
    //  Error detail: from the problem to the end of ITS line (not the rest of the file).
    private static void ValidateClosures(List<ICodeLine> fileLines, List<ISyntaxError> syntaxErrors, IFileDetail fileDetail)
    {
        var scanner = new TSqlScanner();
        ICodeLine? openedOn = null;
        int openedAt = -1;

        foreach (var line in fileLines)
        {
            scanner.ScanLine(line.OriginalContent,
                regionStarts: (index, region) =>
                {
                    if (region is not (SqlRegion.Code or SqlRegion.EolComment))
                        (openedOn, openedAt) = (line, index);
                },
                strayCommentEnd: index =>
                    syntaxErrors.Add(new SyntaxError("Unexpected End of Block-Comment (*/ without a matching /*).", line.OriginalContent.Substring(index), line.LineNumber, fileDetail)));
        }

        string? nonTerminated = scanner.Region switch
        {
            SqlRegion.String => "Non-Terminated String.",
            SqlRegion.BlockComment => "Non-Terminated Block-Comment.",
            SqlRegion.BracketedIdentifier => "Non-Terminated [Identifier].",
            SqlRegion.QuotedIdentifier => "Non-Terminated \"Identifier\".",
            _ => null      // Code: nothing left open.
        };

        if (nonTerminated != null && openedOn != null)
            syntaxErrors.Add(new SyntaxError(nonTerminated, openedOn.OriginalContent.Substring(openedAt), openedOn.LineNumber, fileDetail));
    }
}