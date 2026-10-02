using tsmake.workers;

namespace tsmake.data_models;

public interface IResult
{
    bool HasErrors { get; }
    Exception Exception { get; }
    OperationType OperationType { get; }

    DateTime Start { get; }
    DateTime End { get; }

    List<ISyntaxError> SyntaxErrors { get; }
    List<IConfigError> ConfigErrors { get; }
    List<IArtifact> Artifacts { get; }
    IAssembler Assembler { get; }

    int FileCount { get; }
    int CodeLineCount { get; }
    int DirectivesCount { get; }

    string BuildRoot { get; }
    SourceType BuildRootSourceType { get; }

    void AddAssembler(IAssembler assembler);    
    void SetComplete();
    void SetSyntaxErrors(List<ISyntaxError> errors);
    void SetConfigErrors(List<IConfigError> errors);  
    void AddArtifact(IArtifact artifact);
    void SetBuildRoot(string path, SourceType sourceType);  

    void AddException(Exception ex);
    void SetStatistics(int fileCount, int codeLineCount, int directivesCount);   
}

public class BaseResult : IResult
{
    public bool HasErrors => this.SyntaxErrors.Count > 0 || this.ConfigErrors.Count > 0;
    public Exception Exception { get; private set; }
    public OperationType OperationType { get; }
    public List<ISyntaxError> SyntaxErrors { get; private set; } = new List<ISyntaxError>();
    public List<IConfigError> ConfigErrors { get; private set; } = new List<IConfigError>();
    public IAssembler Assembler { get; private set; } = null!;

    public int FileCount { get; private set; }
    public int CodeLineCount { get; private set; }
    public int DirectivesCount { get; private set; }
    public string BuildRoot { get; private set; }
    public SourceType BuildRootSourceType { get; private set; }
    public List<IArtifact> Artifacts { get; } = new List<IArtifact>();
    public DateTime Start { get; }
    public DateTime End { get; protected set; }

    protected BaseResult(OperationType operationType)
    {
        this.Start = DateTime.Now;
        this.OperationType = operationType;
        this.Exception = null!;
        this.BuildRoot = string.Empty;
    }

    public void SetSyntaxErrors(List<ISyntaxError> errors)
    {
        this.SyntaxErrors = errors;
    }

    public void SetConfigErrors(List<IConfigError> errors)
    {
        this.ConfigErrors = errors;
    }

    public void AddArtifact(IArtifact artifact)
    {
        this.Artifacts.Add(artifact);
    }

    public void SetBuildRoot(string path, SourceType sourceType)
    {
        this.BuildRoot = path;
        this.BuildRootSourceType = sourceType;
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