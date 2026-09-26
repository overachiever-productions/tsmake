namespace tsmake.data_models;

public interface IError
{
    string FileName { get; }
    int LineNumber { get; }
    string Message { get; }
    string Detail { get; }
    ErrorSeverity Severity { get; }
    Stack<IStackEntry> Stack { get; }   
    string Summarize();     // used for xml formatter/CLI output.
}

public class BaseError(string message, string detail, string fileName, int lineNumber, Stack<IStackEntry> stack, ErrorSeverity severity) : IError
{
    private ErrorType ErrorType { get; set; }
    public string Message { get; } = message;
    public string FileName { get; } = fileName;
    public int LineNumber { get; } = lineNumber;
    public string Detail { get; } = detail;
    public Stack<IStackEntry> Stack { get; } = stack;
    public ErrorSeverity Severity { get; } = severity;

    public string Summarize()
    {
        // switch on the TYPE of concrete class. 
        this.ErrorType = this switch
        {
            SyntaxError => ErrorType.Syntax,
            ConfigError => ErrorType.Config,
            _ => ErrorType.Runtime
        };

        return $"{this.ErrorType}: {this.Message} (File: {this.FileName}, Line: {this.LineNumber})";
    }
}

public class SyntaxError(string message, string detail, string fileName, int lineNumber, Stack<IStackEntry> stack, ErrorSeverity severity) 
    : BaseError(message, detail, fileName, lineNumber, stack, severity) { }

// probably need a diff .ctor for this. 
//  i believe that the stack will always be JUST the build file. 
//   and i won't always have a line number ...  ... 
//   but i will (or should) have a List<IRankedString> to determine WHERE the value in question was set (config file, build file, command-line).

// TODO: OR, instead of the above notes about different .ctors ... 
//    it might make sense to have 2x different interfaces for IError ... one for SyntaxError and one for ConfigError ...
//    i.e., don't waste time trying to make a carburetor and a monkey fit the same interface. 
public class ConfigError(string message, string detail, string fileName, int lineNumber, Stack<IStackEntry> stack, ErrorSeverity severity)
    : BaseError(message, detail, fileName, lineNumber, stack, severity) { }