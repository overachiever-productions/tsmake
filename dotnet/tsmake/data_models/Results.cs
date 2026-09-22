namespace tsmake.data_models;

public interface IResult
{
    bool HasErrors { get; }
    OperationType OperationType { get; }

    List<ISyntaxError> Errors { get; }
    List<IArtifact> Artifacts { get; }

    DateTime Start { get; }
    DateTime End { get; }
}

public class BaseResult : IResult
{
    public bool HasErrors => this.Errors.Count > 0;
    public OperationType OperationType { get; }
    public List<ISyntaxError> Errors { get; } = new List<ISyntaxError>();
    public List<IArtifact> Artifacts { get; } = new List<IArtifact>();
    public DateTime Start { get; }
    public DateTime End { get; protected set; }

    protected BaseResult(OperationType operationType)
    {
        this.Start = DateTime.Now;
        this.OperationType = operationType;
    }

    public void AddError(ISyntaxError error)
    {
        this.Errors.Add(error);
    }

    public void AddArtifact(IArtifact artifact)
    {
        this.Artifacts.Add(artifact);
    }

    public void SetComplete()
    {
        this.End = DateTime.Now;
    }
}

public class BuildResult() : BaseResult(OperationType.Build) { }