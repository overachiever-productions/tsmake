namespace tsmake;

public interface IError
{
    //Exception? Exception { get; }

    string Message { get; }
    
    string Summarize();     // used for xml formatter/CLI output.
}

public interface INormalizer
{
    List<IError> Errors { get; }
    int[] LineEndings { get; }
    List<string> Lines { get; }

    string Normalize(string input);
}