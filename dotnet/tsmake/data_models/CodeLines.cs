namespace tsmake.data_models;

public interface ICodeLine
{
    string Content { get; }
    string FileName { get; }
    int LineNumber { get; }
    int StartOffset { get; }
    int EndOffset { get; }
    Stack<IStackEntry> Stack { get; } 
}

public class CodeLine(string content, string fileName, int lineNumber, int startOffset, int endOffset, Stack<IStackEntry> stack) : ICodeLine
{
    public string Content { get; } = content;
    public string FileName { get; } = fileName;
    public int LineNumber { get; } = lineNumber;
    public int StartOffset { get; } = startOffset;
    public int EndOffset { get; } = endOffset;
    public Stack<IStackEntry> Stack { get; } = stack;               
}

public interface IStackEntry
{
    string FilePath { get; }
    int ParentLineNumber { get; }
    int Depth { get; }
}

public class StackEntry(string filePath, int parentLineNumber, int depth) : IStackEntry
{
    public string FilePath { get; } = filePath;
    public int ParentLineNumber { get; } = parentLineNumber;
    public int Depth { get; } = depth;  
}