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

public class SyntaxError(string message, string detail, int lineNumber, Stack<IStackEntry> stack) : ISyntaxError
{
    public string Message { get; } = message;
    public string FileName { get; } = stack.Peek().FilePath;
    public int LineNumber { get; } = lineNumber;
    public string Detail { get; } = detail;
    public Stack<IStackEntry> Stack { get; } = stack;

    public string Summarize()
    {
        return $"{this.Message}: {this.Detail} => {this.FileName}::{this.LineNumber}";
    }
}

public interface IConfigError
{
    string Message { get; }
    string Detail { get; }
    List<IRankedString> RankedStrings { get; }  // think i'll need to search these for the one? with a ##directive? 

    string Summarize();
}

public class ConfigError(string message, string detail, List<IRankedString> rankedStrings) : IConfigError
{
    public string Message { get; } = message;
    public string Detail { get; } = detail;
    public List<IRankedString> RankedStrings { get; } = rankedStrings;
    public string Summarize()
    {
        return $"{this.Message} {this.Detail} => {string.Join(", ", this.RankedStrings.Select(rs => $"[{rs.SourceType}] '{rs.Value}'"))}";
    }
}