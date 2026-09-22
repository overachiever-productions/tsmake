namespace tsmake.data_models;

public interface IAssemblerOptions
{
    CommentRemovalDirectives CommentRemovalDirectives { get; }
    TokenReplacementDirectives TokenReplacementDirectives { get; }
    ITokenDefinitionRegistry TokenDefinitionRegistry { get; }

}

public class AssemblerOptions(ITokenDefinitionRegistry tokenDefinitionRegistry) : IAssemblerOptions
{
    public CommentRemovalDirectives CommentRemovalDirectives { get; } = new CommentRemovalDirectives();
    public TokenReplacementDirectives TokenReplacementDirectives { get; } = new TokenReplacementDirectives();
    public ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
}