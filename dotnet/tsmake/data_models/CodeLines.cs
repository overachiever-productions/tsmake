namespace tsmake.data_models;

public interface ICodeLine
{
    string OriginalContent { get; }
    string TransformedContent { get; }
    int LineNumber { get; }
    int StartOffset { get; }
    int EndOffset { get; }
    IDirective Directive { get; internal set; }
    IFileDetail FileDetail { get; }
}

public class CodeLine(string content, int lineNumber, int startOffset, int endOffset, IFileDetail fileDetail) : ICodeLine
{
    public string OriginalContent { get; } = content;
    public string TransformedContent { get; private set; } = string.Empty;
    public int LineNumber { get; } = lineNumber;
    public int StartOffset { get; } = startOffset;
    public int EndOffset { get; } = endOffset;
    public IDirective Directive { get; set; } = null!;
    public IFileDetail FileDetail { get; } = fileDetail;
}

public interface IFileDetail
{
    string FilePath { get; }
    int ParentLineNumber { get; }
    int Depth { get; }
}

public class FileDetail(string filePath, int parentLineNumber, int depth) : IFileDetail
{
    public string FilePath { get; } = filePath;
    public int ParentLineNumber { get; } = parentLineNumber;
    public int Depth { get; } = depth;
}