namespace tsmake.data_models;

public interface IAssemblerOptions
{
    string OutputPath { get; }
    string RootPath { get; }

    OperationType OperationType { get; }
    LineEndingsType LineEndingsType { get; }

    CommentRemovalDirectives CommentRemovalDirectives { get; }
    TokenExclusionDirectives TokenExclusionDirectives { get; }
    ITokenDefinitionRegistry TokenDefinitionRegistry { get; }

    void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives);
    void SetOutputPath(string outputPath);
}

public class AssemblerOptions(ITokenDefinitionRegistry tokenDefinitionRegistry, OperationType operationType) : IAssemblerOptions
{
    public string OutputPath { get; private set; } = null!;
    public string RootPath { get; private set; } = null!;
    public OperationType OperationType { get; } = operationType;

    public LineEndingsType LineEndingsType { get; private set; } = LineEndingsType.CrLf;
    public CommentRemovalDirectives CommentRemovalDirectives { get; private set; } = CommentRemovalDirectives.None;
    public TokenExclusionDirectives TokenExclusionDirectives { get; private set; } = TokenExclusionDirectives.None;
    public ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    public void SetOutputPath(string outputPath)
    {
        this.OutputPath = outputPath;
    }

    public void SetDirectives(LineEndingsType lineEndingsType, CommentRemovalDirectives commentRemovalDirectives, TokenExclusionDirectives tokenExclusionDirectives)
    {
        this.LineEndingsType = lineEndingsType;
        this.CommentRemovalDirectives = commentRemovalDirectives;
        this.TokenExclusionDirectives = tokenExclusionDirectives;
    }
}