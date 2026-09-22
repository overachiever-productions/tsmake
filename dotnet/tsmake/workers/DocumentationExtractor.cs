using tsmake.data_models;

namespace tsmake.workers;

public interface IDocumentationExtractor
{
    void ExtractDocumentation(List<ICodeLine> codeLines, List<ICodeLine> docLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack);
}

public class DocumentationExtractor : IDocumentationExtractor
{
    public void ExtractDocumentation(List<ICodeLine> codeLines, List<ICodeLine> docLines, List<ISyntaxError> syntaxErrors, Stack<IStackEntry> stack)
    {
        // NOTE:
        //     codeLines is the SOURCE... 
        //    docLines is the DESTINATION... (i.e., it's EMPTY and passed in by the parent/caller so that we can COPY docComments into the "output".


        throw new NotImplementedException();
    }
}