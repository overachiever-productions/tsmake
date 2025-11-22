namespace tsmake;

public class NormalizerOptions(LineEndingOptions lineEndingOptions = LineEndingOptions.CrLf, GoOptions goOptions = GoOptions.None)
{
    public LineEndingOptions LineEndings { get; set; } = lineEndingOptions;
    public GoOptions Go { get; set; } = goOptions;
}

public class Normalizer(NormalizerOptions normalizerOptions) : INormalizer
{
    private string _input = "";
    private readonly NormalizerOptions _normalizerOptions = normalizerOptions;

    public List<IError> Errors => new List<IError>();
    public bool HasErrors => this.Errors.Count > 0;

    public int[] LineEndings => throw new NotImplementedException();
    public List<string> Lines => throw new NotImplementedException();

    public string Normalize(string input)
    {
        this._input = input;

        // regex split... 
        // populate .Lines + .LineEndings
        // and then ... collapse back to string based on options + output.

        throw new NotImplementedException();
    }
}
