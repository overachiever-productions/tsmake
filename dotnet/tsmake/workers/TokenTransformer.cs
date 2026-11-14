using tsmake.data_models;

namespace tsmake.workers;

public interface ITokenTransformer
{
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34
    void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors);
}

public class TokenTransformer(TokenExclusionDirectives tokenExclusionDirectives, ITokenDefinitionRegistry tokenDefinitionRegistry) : ITokenTransformer
{
    private TokenExclusionDirectives TokenExclusionDirectives { get; } = tokenExclusionDirectives;
    private ITokenDefinitionRegistry TokenDefinitionRegistry { get; } = tokenDefinitionRegistry;
    
    // IMPLEMENTATION: https://overachieverllc.atlassian.net/browse/TSM-34

    public void TransformTokens(List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
    {
        // 1. iterate over each codeline - looking for TOKENS - i.e., 'compiled' regex matching the token pattern: {{##TOKEN_NAME}}
        //              i.e., we could be iterating over 10s of thousands of lines of code - so we need to be efficient.

        // 2. When we find a token, we need to review this.TokenExclusionDirectives to determine how to proceed: 
        //   if TokenExclusionDirectives.None - then, no exclusions - we can transform the token.
        //   if TokenExclusionDirectives.ExcludeEolComments - then, if the token is within an EOL comment, we should skip it. 
        //          this is trivial to discern - as an EOL (end of line) comment is line-specific.
        //   However, if the token is within a /* Block Comment */ or within a 'string', - i.e., TokenExclusionDirectives.ExcludeStrings or TokenExclusionDirectives.ExcludeBlockComments
        //         then ... this'll be a bit harder to determine - because strings and block comments can be multi-line
        //         some POTENTIAL options for ways to handle this: 
        //                 a. we could 'convert' the codeLines into a single string - and then use regex to find all the strings and block comments - and then 'mark' those lines as being within a string or block comment.
        //                 b. OR ... as we iterate over each line, keep track of whether we're currently within a string or block comment -
        //                      and then EITHER mark those code-lines as HAVING .string or .blockComment 'markers' (start, end, entire line, etc.) 
        //                      OR BETTER OPTION: just keep track of the 'state' of whether we're currently within a string or block comment - and then when we find a token, we can check the current state to determine if we're within a string or block comment.   

        // 3. If we find a token that is NOT excluded by the TokenExclusionDirectives, then we need to check the TokenDefinitionRegistry to see if we have a definition for this token.
        //      specifically, check for a match in this.TokenDefinitionRegistry.DefinedTokens - if we find a match for the token KEY, then we can swap out the token with the specified value.
        //          e.g., our REGEX will match tokens as {{##TOKEN_NAME}} - and then we'll check for a match in this.TokenDefinitionRegistry.DefinedTokens for the key "##TOKEN_NAME" (i.e., without the {{ }} brackets).
        //     if/when we find a match and do a swap, the SWAP will 'occur' or be tracked within the codeLine's TransformedContent property - which will be updated with the new value.
    }
}