namespace tsmake.data_models;

public interface IAssemblerOptions
{
    List<IRankedString> BuildRoot { get; }
    List<IRankedString> BuildOutput { get; }
    List<IRankedString> FileMarkerPath { get; }

    OperationType OperationType { get; }
    LineEndingsType LineEndingsType { get; }

    CommentRemovalDirectives CommentRemovalDirectives { get; }
    TokenExclusionDirectives TokenExclusionDirectives { get; }
    ITokenDefinitionRegistry TokenDefinitionRegistry { get; }

    void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives);
    void AddOutputPath(IRankedString rankedString);
    void AddRootPath(IRankedString rankedString);
    void AddFileMarkerPath(IRankedString rankedString);
}

public class AssemblerOptions(ITokenDefinitionRegistry tokenDefinitionRegistry, OperationType operationType) : IAssemblerOptions
{
    public List<IRankedString> BuildRoot { get; set; } = new List<IRankedString>();
    public List<IRankedString> BuildOutput { get; set; } = new List<IRankedString>();
    public List<IRankedString> FileMarkerPath { get; set; } = new List<IRankedString>();

    public OperationType OperationType { get; } = operationType;

    public LineEndingsType LineEndingsType { get; private set; } = LineEndingsType.CrLf;
    public CommentRemovalDirectives CommentRemovalDirectives { get; private set; } = CommentRemovalDirectives.None;
    public TokenExclusionDirectives TokenExclusionDirectives { get; private set; } = TokenExclusionDirectives.None;
    public ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    public void AddOutputPath(IRankedString rankedString)
    {
        this.BuildOutput.Add(rankedString);
    }

    public void AddRootPath(IRankedString rankedString)
    {
        this.BuildRoot.Add(rankedString);
    }

    public void AddFileMarkerPath(IRankedString rankedString)
    {
        this.FileMarkerPath.Add(rankedString);
    }

    public void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives)
    {
        this.LineEndingsType = lineEndingsType;
        this.CommentRemovalDirectives = commentRemovalDirectives;
        this.TokenExclusionDirectives = tokenExclusionDirectives;
    }
}