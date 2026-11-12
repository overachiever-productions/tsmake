namespace tsmake.data_models;

// TODO: 
//      SINCE I'm going to be able to 'pinpoint' errors down to the FILE and specific LINE where they 'occur'... 
//       No reason to NOT have logic/functionality similar to admindb's dbo.extract_code_lines ...
//       Where I:
//          1. grab the line in question. 
//          2. and also grab ~4 lines before and after (when present)
//          3. 'point at'/to the problem line with the whole --> against a left-adjusted do-hickie - like I'm doing with dbo.extract_code_lines. 


public interface ISyntaxError
{
    string FileName { get; }
    int LineNumber { get; }
    string Message { get; }
    string Detail { get; }
    //Stack<IStackEntry> Stack { get; }   
    IFileDetail FileDetail { get; }

    string Summarize();     // used for xml formatter/CLI output.
}


public class SyntaxError(string message, string detail, int lineNumber, IFileDetail fileDetail)
    : ISyntaxError
{
    public string Message { get; } = message;
    public string FileName { get; } = fileDetail.FilePath;
    public int LineNumber { get; } = lineNumber;
    public string Detail { get; } = detail;
    public IFileDetail FileDetail { get; } = fileDetail;

    public SyntaxError(string message, string detail, ICodeLine codeLine)
        : this(message, detail, codeLine.LineNumber, codeLine.FileDetail)
    { }

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