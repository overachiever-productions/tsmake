using tsmake.data_models;

namespace tsmake.workers;

public interface ITokenTransformer
{
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34
    void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, ITokenDefinitionRegistry optionsTokenDefinitionRegistry, TokenExclusionDirectives optionsTokenExclusionDirectives);
}

public class TokenTransformer(TokenExclusionDirectives directives) : ITokenTransformer
{
    private TokenExclusionDirectives Directives { get; } = directives;
    
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34

    public void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, ITokenDefinitionRegistry optionsTokenDefinitionRegistry, TokenExclusionDirectives optionsTokenExclusionDirectives)
    {
        // NOTE: tokens may/may-not be transformed within 'strings', --comments, or /* comments */ - depending upon directives. 

        // per each codeLine... 
        //    check for tokens - and exclude any that are within strings or comments (depending upon TokenExclusionDirectives). 
        //
        //   if we're still a match for a token: 
        //    lookup vs the TokenDefinitionRegistry to see if we have a match for a token.
        //   if not ... 
        //     see if the token has a valid default and/or allows defaults
        //          if so... swap token out with default and move on. 
        //   if we DO find a token ... 
        //      replace. 


        // AND ... each time we 'swap out' a token ... we need to update the codeLine's TransformedContent property.

    }
}