namespace tsmake.data_models;

public interface IAssemblerOptions
{
    List<IRankedString> BuildRoot { get; }
    List<IRankedString> BuildOutput { get; }
    List<IRankedString> FileMarkerPath { get; }

    List<string> Flags { get; }
    FlagsSource FlagsSource { get; }

    OperationType OperationType { get; }
    LineEndingsType LineEndingsType { get; }

    CommentRemovalDirectives CommentRemovalDirectives { get; }
    TokenExclusionDirectives TokenExclusionDirectives { get; }
    ITokenDefinitionRegistry TokenDefinitionRegistry { get; }

    void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives);
    void SetFlags(List<string> flags, FlagsSource source);
    void AddOutputPath(IRankedString rankedString);
    void AddRootPath(IRankedString rankedString);
    void AddFileMarkerPath(IRankedString rankedString);
    
}

public class AssemblerOptions(ITokenDefinitionRegistry tokenDefinitionRegistry, OperationType operationType) : IAssemblerOptions
{
    public List<IRankedString> BuildRoot { get; set; } = new List<IRankedString>();
    public List<IRankedString> BuildOutput { get; set; } = new List<IRankedString>();
    public List<IRankedString> FileMarkerPath { get; set; } = new List<IRankedString>();
    public List<string> Flags { get; set; } = new List<string>();
    public FlagsSource FlagsSource { get; set; } = FlagsSource.NotSet;

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

    public void SetFlags(List<string> flags, FlagsSource source)
    {
        this.Flags = flags;
        this.FlagsSource = source;
    }

    public void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives)
    {
        this.LineEndingsType = lineEndingsType;
        this.CommentRemovalDirectives = commentRemovalDirectives;
        this.TokenExclusionDirectives = tokenExclusionDirectives;
    }
}