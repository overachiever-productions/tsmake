using tsmake.data_models;

namespace tsmake.workers;

public interface IDirectiveProcessor
{    
    void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, 
        Stack<IStackEntry> stack, string buildRoot, string buildOutput);
}

public class DirectiveProcessor : IDirectiveProcessor
{
    public void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors, 
        Stack<IStackEntry> stack, string buildRoot, string buildOutput)
    {
        // NOTE: .buildRoot and .buildOutput are passed by REF... 
        //  that said ... if they're NOT EMPTY ... 
        //      then they should NOT be set in here - i.e., there's a higher-level directive that has already set them.
        //      hierarchy for these 2 values is (lowest to highest): .sql file, config file, command-line args.

        // Processing Order: 
        // a. check for root/output directives ... if found ... set the values (unless already set).
        // b. check for conditional-include directives ... if found (convert to FILE|DIRECTORY) 
        // c. check for conditional-include conditions  .. and process BUILD or DEPLOY-TIME logic as needed. 
        // d. process INCLUDES (FILE|DIRECTORY) ... 
        //      and FOREACH INCLUDED file ... recurse (against the parent). 
        //          string newChildFilePath = "";
        //          int lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile = -99;
        //          parent.RecursivelyAssemble(newChildFilePath, lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile);

        //throw new NotImplementedException();
    }
}