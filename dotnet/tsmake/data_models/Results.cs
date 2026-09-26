using tsmake.workers;

namespace tsmake.data_models;

public interface IResult
{
    bool HasErrors { get; }
    Exception Exception { get; }
    OperationType OperationType { get; }

    DateTime Start { get; }
    DateTime End { get; }

    List<IError> Errors { get; }
    List<IArtifact> Artifacts { get; }
    IAssembler Assembler { get; }

    int FileCount { get; }
    int CodeLineCount { get; }
    int DirectivesCount { get; }
    
    void AddAssembler(IAssembler assembler);    
    void SetComplete();
    void SetErrors(List<IError> errors);
    void AddArtifact(IArtifact artifact);

    void AddException(Exception ex);
    void SetStatistics(int fileCount, int codeLineCount, int directivesCount);   
}

public class BaseResult : IResult
{
    public bool HasErrors => this.Errors.Count > 0;
    public Exception Exception { get; private set; }
    public OperationType OperationType { get; }
    public List<IError> Errors { get; private set; } = new List<IError>();
    public IAssembler Assembler { get; private set; } = null!;

    public int FileCount { get; private set; }
    public int CodeLineCount { get; private set; }
    public int DirectivesCount { get; private set; }
    public List<IArtifact> Artifacts { get; } = new List<IArtifact>();
    public DateTime Start { get; }
    public DateTime End { get; protected set; }

    protected BaseResult(OperationType operationType)
    {
        this.Start = DateTime.Now;
        this.OperationType = operationType;
        this.Exception = null!;
    }

    public void AddError(IError error)
    {
        this.Errors.Add(error);
    }

    public void SetErrors(List<IError> errors)
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
    
    public void SetStatistics(int fileCount, int codeLineCount, int directivesCount)
    {
        this.FileCount = fileCount;
        this.CodeLineCount = codeLineCount;
        this.DirectivesCount = directivesCount;
    }

    public void AddAssembler(IAssembler assembler)
    {
        this.Assembler = assembler;
    }

    public void SetComplete()
    {
        this.End = DateTime.Now;
    }
}

public class BuildResult() : BaseResult(OperationType.Build) { }