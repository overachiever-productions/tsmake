namespace tsmake;

public interface IError
{
    ErrorRecord ErrorRecord { get; }

    string Message { get; }
    
    string Summarize();     // used for xml formatter/CLI output.
}

public interface INormalizer
{
    List<IError> Errors { get; }
    bool HasErrors { get; }

    int[] LineEndings { get; }
    List<string> Lines { get; }

    string Normalize(string input);
}