namespace tsmake;

public interface IError
{
    ErrorType ErrorType { get; }
    ErrorRecord ErrorRecord { get; }
    
    ISourceLine SourceLine { get; } // i.e., the location 

    string Phase { get; }
    string Facet { get; }
    string Detail { get; }

    string Message { get; }

    string Summarize();      // used by xml formatters/etc. 
}

public class Error : IError
{
    public ErrorType ErrorType { get; }
    public ErrorRecord ErrorRecord { get; }
    public ISourceLine SourceLine { get; }
    public string Phase { get; }
    public string Facet { get; }
    public string Detail { get; }
    public string Message { get; }

    private Error(ErrorRecord errorRecord, ErrorType type, SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        this.ErrorType = type;
        this.ErrorRecord = errorRecord;
        this.SourceLine = sourceLine;
        this.Phase = phase;
        this.Facet = facet;
        this.Detail = detail;
        this.Message = message;
    }

    private Error(ErrorType type, SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        this.ErrorType = type;
        this.SourceLine = sourceLine;
        this.Phase = phase;
        this.Facet = facet;
        this.Detail = detail;
        this.Message = message;
    }

    public static Error NewRuntimeError(ErrorRecord errorRecord, SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        return new Error(errorRecord, ErrorType.Runtime, sourceLine, phase, message, facet, detail);
    }

    public static Error NewConfigurationError(ErrorRecord errorRecord, SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        return new Error(errorRecord, ErrorType.Configuration, sourceLine, phase, message, facet, detail);
    }

    public static Error NewValidationError(SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        return new Error(ErrorType.Validation, sourceLine, phase, message, facet, detail);
    }

    public static Error NewSyntaxError(SourceLine sourceLine, string phase, string message, string facet = "", string detail = "")
    {
        return new Error(ErrorType.Syntax, sourceLine, phase, message, facet, detail);
    }

    //public static Error FakeError(ErrorRecord errorRecord)
    //{
    //    return new Error(ErrorType.Runtime, null, "fake", "doh", "doh2", "doh3");
    //}

    //public static Error NewBuildError()
    //{
    //}

    //public static Error NewDocumentationError()
    //{
    //}

    public string GetErrorTitle()
    {
        return $"{this.ErrorType.ToString().ToUpperInvariant()} ERROR";
    }

    public string Summarize()
    {
        throw new NotImplementedException();

        // something along the lines of Option C makes the most sense... 


        // OPTION A (phase is a different line)
        // {ErrorType} Error:
        //    <phase> (<facet if there is one>)
        //      <file-name.ext>, line ##  (offsets if i have them)
        //      <message> 
        //      <detail/etc.>

        // OPTION B (phase is part of the error 'type')
        // {ErrorType} Error | {Phase (facet if there is one)} 
        //      {location stuff - as above}
        //      {message}
        //      {detail}

        // OPTION C - phases as a GROUPING mechanism out/above error types: 
        // {Phase}
        //      {ErrorType} Error: 
        //          {Facet} 
        //          {location}
        //          {message}
        //          {detail}
    }
}