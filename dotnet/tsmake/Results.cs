namespace tsmake;

public interface IArtifact
{
    string Path { get; }
    ArtifactType ArtifactType { get; }
    DateTime Persisted { get; }
}

public interface IResult
{
    bool HasErrors { get; }
    OperationType OperationType { get; }
 
    List<IError> Errors { get; }
    List<IArtifact> Artifacts { get; }

    DateTime Start { get; }
    DateTime End { get; }
}

public class BaseResult : IResult
{
    public bool HasErrors => this.Errors.Count > 0;
    public OperationType OperationType { get; }
    public List<IError> Errors { get; } = new List<IError>();
    public List<IArtifact> Artifacts { get; } = new List<IArtifact>();
    public DateTime Start { get; } = DateTime.Now;
    public DateTime End { get; protected set; }

    protected BaseResult(OperationType operationType)
    {
        this.OperationType = operationType;
    }

    public void AddError(IError error)
    {
        this.Errors.Add(error);
    }

    public void AddErrors(List<IError> errors)
    {
        this.Errors.AddRange(errors);
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

// this can be used for EITHER a) builds, b) docs, c) builds and docs. 
public class BuildResult(Verb verb, string buildFile) : BaseResult(OperationType.Build)
{
    public Verb Verb { get; } = verb;
    public string BuildFile { get;  } = buildFile;
}

public class BuildWrapper
{
    public List<IResult> Results { get; } = new List<IResult>();

    public List<IError> Errors
    {
        get
        {
            var output = new List<IError>();
            foreach(var result in this.Results)
                output.AddRange(result.Errors);

            return output;
        }
    }

    public void AddResult(IResult added)
    {
        this.Results.Add(added);
    }
}

// TODO: add the following result types - eventually: 
//  MigrationResult (i.e., RunnerResult or whatever is needed to handle executions). 
//  GeneratorResult (i.e., what happens when you instruct tsmake to create a new build.sql file from sourc-control or whatever. 