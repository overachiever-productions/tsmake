namespace tsmake;

public class SyntaxError : ISyntaxError {
    
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string Message { get; }

    public SyntaxError(string message, int start, int end)
    {
        this.Message = message;
        this.StartOffset = start;
        this.EndOffset = end;
    }

    public void SetLocation(ILocation location)
    {
        throw new NotImplementedException();
    }
    
    public string Summarize()
    {
        throw new NotImplementedException();
    }
}