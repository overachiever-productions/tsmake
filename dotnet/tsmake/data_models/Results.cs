namespace tsmake.data_models;

public interface IResult
{
    bool HasErrors { get; }
    Exception Exception { get; }
    OperationType OperationType { get; }

    DateTime Start { get; }
    DateTime End { get; }

    List<ISyntaxError> Errors { get; }
    List<IArtifact> Artifacts { get; }

    int FileCount { get; }
    int CodeLineCount { get; }

    void SetComplete();
    void SetErrors(List<ISyntaxError> errors);
    void AddArtifact(IArtifact artifact);

    void AddException(Exception ex);
    void SetStatistics(int fileCount, int codeLineCount);   
}

public class BaseResult : IResult
{
    public bool HasErrors => this.Errors.Count > 0;
    public Exception Exception { get; private set; }
    public OperationType OperationType { get; }
    public List<ISyntaxError> Errors { get; private set; } = new List<ISyntaxError>();
    public int FileCount { get; private set; }
    public int CodeLineCount { get; private set; }
    public List<IArtifact> Artifacts { get; } = new List<IArtifact>();
    public DateTime Start { get; }
    public DateTime End { get; protected set; }

    protected BaseResult(OperationType operationType)
    {
        this.Start = DateTime.Now;
        this.OperationType = operationType;
        this.Exception = null!;
    }

    public void AddError(ISyntaxError error)
    {
        this.Errors.Add(error);
    }

    public void SetErrors(List<ISyntaxError> errors)
    {
        this.Errors = errors;
    }

    public void AddArtifact(IArtifact artifact)
    {
        this.Artifacts.Add(artifact);
    }

    public void AddException(Exception ex)
    {
        this.Exception = ex;
    }
    
    public void SetStatistics(int fileCount, int codeLineCount)
    {
        this.FileCount = fileCount;
        this.CodeLineCount = codeLineCount;
    }

    public void SetComplete()
    {
        this.End = DateTime.Now;
    }
}

public class BuildResult() : BaseResult(OperationType.Build) { }