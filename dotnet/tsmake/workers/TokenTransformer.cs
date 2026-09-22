using tsmake.data_models;

namespace tsmake.workers;

public interface ITokenTransformer
{
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34
    void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack, ITokenDefinitionRegistry optionsTokenDefinitionRegistry, TokenReplacementDirectives optionsTokenReplacementDirectives);
}

public class TokenTransformer(TokenReplacementDirectives directives) : ITokenTransformer
{
    private TokenReplacementDirectives Directives { get; } = directives;
    
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34

    public void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack, ITokenDefinitionRegistry optionsTokenDefinitionRegistry, TokenReplacementDirectives optionsTokenReplacementDirectives)
    {
        // NOTE: tokens may/may-not be transformed within 'strings', --comments, or /* comments */ - depending upon directives. 


        throw new NotImplementedException();
    }
}