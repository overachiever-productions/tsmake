namespace tsmake;

public class Normalizer : INormalizer
{
    private readonly string _input;
    private readonly LineEndingOptions _lineEndingOptions;

    public List<CodeLine> Lines { get; }

    public string NormalizedText { get; private set; }

    public Normalizer(string input, LineEndingOptions lineEndingOptions = LineEndingOptions.CrLf)
    {
        this.Lines = new List<CodeLine>();
        this.NormalizedText = string.Empty;

        this._input = input ?? string.Empty;
        this._lineEndingOptions = lineEndingOptions;
        
        this.Normalize();
    }

    private void Normalize()
    {
        var regex = new Regex(@"\r\n|\r|\n", Global.SingleLineRegexOptions);
        var matches = regex.Matches(this._input);

        int lineNumber = 1;
        int previousStart = 0;
        if (matches.Count < 1)
        {
            this.Lines.Add(new CodeLine(lineNumber, 1, this._input.Length, this._input));
            this.NormalizedText = this.SerializeLines();
            return;
        }

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

            CodeLine line = new CodeLine(lineNumber, oneBasedLineStart, oneBasedLineEnd, lineText);
            this.Lines.Add(line);

            previousStart = lineEnd + match.Length;
            lineNumber++;
        }

        var finalMatch = matches[^1];
        if (finalMatch.Index + finalMatch.Length == this._input.Length) // non-obvious logic here, but it's _NEEDED_.
            this.Lines.Add(new CodeLine(lineNumber, previousStart, previousStart, ""));

        if (previousStart < this._input.Length)
            this.Lines.Add(new CodeLine(lineNumber, previousStart + 1, this._input.Length, this._input.Substring(previousStart, this._input.Length - previousStart)));

        this.NormalizedText = this.SerializeLines();
    }

    private string SerializeLines()
    {
        var lineEnding = this._lineEndingOptions switch
        {
            LineEndingOptions.CrLf => "\r\n",
            LineEndingOptions.Lf => "\n",
            LineEndingOptions.Cr => "\r",
            _ => throw new InvalidEnumArgumentException()
        };

        var sb = new StringBuilder();
        int i = 0;
        foreach (var line in this.Lines)
        {
            sb.Append(line.Text);

            if(i != this.Lines.Count - 1) 
                sb.Append(lineEnding);
            
            i++;
        }

        return sb.ToString();
    }
}