namespace tsmake.data_models;

public interface IAssemblerOptions
{
    string GetOutputPath { get; }
    string GetRootPath { get; }

    OperationType OperationType { get; }
    LineEndingsType LineEndingsType { get; }

    CommentRemovalDirectives CommentRemovalDirectives { get; }
    TokenExclusionDirectives TokenExclusionDirectives { get; }
    ITokenDefinitionRegistry TokenDefinitionRegistry { get; }

    void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives);
    void AddOutputPath(IRankedString rankedString);
    void AddRootPath(IRankedString rankedString);
}

public class AssemblerOptions(ITokenDefinitionRegistry tokenDefinitionRegistry, OperationType operationType) : IAssemblerOptions
{
    private List<IRankedString> _outputPath { get; set; } = new List<IRankedString>();
    private List<IRankedString> _rootPath { get; set; } = new List<IRankedString>();
    public OperationType OperationType { get; } = operationType;

    public string GetOutputPath => this._outputPath.OrderBy(p => p.SourceType.Priority()).FirstOrDefault()?.Value ?? string.Empty;
    public string GetRootPath => this._rootPath.OrderBy(p => p.SourceType.Priority()).FirstOrDefault()?.Value ?? string.Empty;

    public LineEndingsType LineEndingsType { get; private set; } = LineEndingsType.CrLf;
    public CommentRemovalDirectives CommentRemovalDirectives { get; private set; } = CommentRemovalDirectives.None;
    public TokenExclusionDirectives TokenExclusionDirectives { get; private set; } = TokenExclusionDirectives.None;
    public ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    public void AddOutputPath(IRankedString rankedString)
    {
        this._outputPath.Add(rankedString);
    }

    public void AddRootPath(IRankedString rankedString)
    {
        this._rootPath.Add(rankedString);
    }

    public void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives)
    {
        this.LineEndingsType = lineEndingsType;
        this.CommentRemovalDirectives = commentRemovalDirectives;
        this.TokenExclusionDirectives = tokenExclusionDirectives;
    }
}