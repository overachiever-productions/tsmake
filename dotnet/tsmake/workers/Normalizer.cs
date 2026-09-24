using tsmake.data_models;

namespace tsmake.workers;

public interface INormalizer
{
    void Normalize(string fileContent, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack);
    string NormalizedText { get; }
}

public class Normalizer(LineEndingsType lineEndingsType = LineEndingsType.CrLf) : INormalizer
{
    private string _input = string.Empty;
    private readonly LineEndingsType _lineEndingsType = lineEndingsType;

    public string NormalizedText { get; private set; } = string.Empty;

    public void Normalize(string fileContent, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack)
    {
        this._input = fileContent;
        var current = stack.Peek();
        var currentFileName = current.FilePath;

        var regex = new Regex(@"\r\n|\r|\n", Global.SingleLineRegexOptions);
        var matches = regex.Matches(fileContent);

        int lineNumber = 1;
        int previousStart = 0;

        if (matches.Count < 1)
        {
            codeLines.Add(new CodeLine(fileContent, currentFileName, 1, 1, fileContent.Length, stack));
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

                CodeLine line = new CodeLine(lineText, currentFileName, lineNumber, oneBasedLineStart, oneBasedLineEnd, stack);
                codeLines.Add(line);

                previousStart = lineEnd + match.Length;
                lineNumber++;
            }

            var finalMatch = matches[^1];
            if (finalMatch.Index + finalMatch.Length == this._input.Length) // non-obvious logic here, but it's _NEEDED_ for trailing "empty lines".
                codeLines.Add(new CodeLine("", currentFileName, lineNumber, previousStart, previousStart, stack));

            if (previousStart < this._input.Length)
                codeLines.Add(new CodeLine(this._input.Substring(previousStart, this._input.Length - previousStart), currentFileName, lineNumber, previousStart + 1, this._input.Length, stack));
        }

        this.ValidateClosures(syntaxErrors, codeLines, currentFileName, stack);
    }

    private void ValidateClosures(List<ISyntaxError> syntaxErrors, List<ICodeLine> codeLines, string currentFileName, Stack<IStackEntry> stack)
    {
        var lineEnding = this._lineEndingsType switch
        {
            LineEndingsType.CrLf => "\r\n",
            LineEndingsType.Lf => "\n",
            LineEndingsType.Cr => "\r",
            _ => throw new InvalidEnumArgumentException()
        };

        string normalizedText = Regex.Replace(this._input, @"\r\n|\r|\n", lineEnding, Global.SingleLineRegexOptions);
        this.NormalizedText = normalizedText;

        if (string.IsNullOrEmpty(normalizedText))
            return;

        var validEntitiesReplacedWithPlaceHolders = Regex.Replace(normalizedText, @"(?s)(?<String>N?'.*?')|(?<BlockComment>/\*.*?\*/)|(?<BracketedText>\[([^\]]|\]\])*\])", m => new string('x', m.Length), Global.BatchSplittingOptions);

        var pattern = @"(?m)(?i)(?<UnclosedString>'(?:[^']|'')*$)|(?<UnclosedBlockComment>/\*(?:(?!\*/).)*$)|(?<UnclosedBrackets>\[(?:(?!\]).)*$)";
        var regex = new Regex(pattern, Global.BatchSplittingOptions);
        var matches = regex.Matches(validEntitiesReplacedWithPlaceHolders);

        if (matches.Count > 0)
        {
            foreach (Match m in matches)
            {
                foreach (Group g in m.Groups)
                {
                    if (g.Success && "_UnclosedString_UnclosedBlockComment_UnclosedBrackets".IndexOf(g.Name, StringComparison.InvariantCultureIgnoreCase) > 0)
                    {
                        var syntaxErrorLine = codeLines.FirstOrDefault(cl => (cl.FileName == currentFileName) && (cl.StartOffset <= g.Index) && (cl.EndOffset >= g.Index));
                        syntaxErrors.Add(new SyntaxError(this.TranslateNonClosedType(g.Name), g.Value, currentFileName, syntaxErrorLine?.LineNumber ?? -99, stack));
                    }
                }
            }
        }
    }

    private string TranslateNonClosedType(string matchName)
    {
        switch (matchName)
        {
            case "UnclosedString":
                return "Non-Terminated String.";
            case "UnclosedBlockComment":
                return "Non-Terminated Block-Comment.";
            case "UnclosedBrackets":
                return "Non-Terminated [Identifier].";
            default:
                throw new InvalidEnumArgumentException();
        }
    }
}