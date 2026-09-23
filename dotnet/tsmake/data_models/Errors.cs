namespace tsmake.data_models;

public interface ISyntaxError
{
    string FileName { get; }
    int LineNumber { get; }
    string Message { get; }
    string Detail { get; }
    Stack<IStackEntry> Stack { get; }   
    string Summarize();     // used for xml formatter/CLI output.
}

public class SyntaxError(string message, string detail, string fileName, int lineNumber, Stack<IStackEntry> stack) : ISyntaxError
{
    public string Message { get; } = message;
    public string FileName { get; } = fileName;
    public int LineNumber { get; } = lineNumber;
    public string Detail { get; } = detail;
    public Stack<IStackEntry> Stack { get; } = stack;

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