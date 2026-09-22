namespace tsmake.data_models;

public interface ISyntaxError
{
    // TODO: v2026 ... just (initially) needs a place-holder for this kind of issue - until I figure out 'stack' and so on... 
    //int StartOffset { get; }
    //int EndOffset { get; }
    string FileName { get; }
    int LineNumber { get; }
    string Message { get; }
    string Summarize();     // used for xml formatter/CLI output.
}

public class SyntaxError(string message, string fileName, int lineNumber) : ISyntaxError
{
    public string Message { get; } = message;
    public string FileName { get; } = fileName;
    public int LineNumber { get; } = lineNumber;

    public string Summarize()
    {
        throw new NotImplementedException();
    }
}

// MKC: v2026 - Probably will NOT use.
// i.e., an EXCEPTION encountered by any build/processing stage
//public interface IBuildError
//{
//    OperationType Operation { get; }
//    //string Facet { get; }
//    //string Detail { get; }

//    string Message { get; }
//    string Summarize();      // used by xml formatters/etc. 
//}