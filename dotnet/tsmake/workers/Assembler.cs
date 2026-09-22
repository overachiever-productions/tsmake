using tsmake.data_models;

namespace tsmake.workers;

public interface IAssembler
{
    string BuildRoot { get; }
    string BuildOutput { get; }
    void Assemble(string buildFilePath);
    internal void RecursivelyAssemble(string filePath, int parentFileLine);
}

public class Assembler(IAssemblerOptions options, IFileSystem fileSystem, INormalizer normalizer, ITokenTransformer tokenTransformer, 
    IDirectiveProcessor directiveProcessor) : IAssembler
{
    private IAssemblerOptions Options { get; } = options;
    private IFileSystem FileSystem { get; } = fileSystem;
    private INormalizer Normalizer { get; } = normalizer;
    private ITokenTransformer TokenTransformer { get; } = tokenTransformer;
    private IDirectiveProcessor DirectiveProcessor { get; } = directiveProcessor;
    private IDocumentationExtractor DocumentationExtractor { get; } = null!;

    private List<ISyntaxError> SyntaxErrors { get; set; } = new List<ISyntaxError>();
    private List<ICodeLine> CodeLines { get; set; } = new List<ICodeLine>();
    private List<ICodeLine> DocLines { get; set; } = new List<ICodeLine>();
    private Stack<IStackEntry> Stack { get; set; } = new Stack<IStackEntry>();

    public Assembler(IAssemblerOptions options, IFileSystem fileSystem, INormalizer normalizer, ITokenTransformer tokenTransformer,
        IDirectiveProcessor directiveProcessor, IDocumentationExtractor documentationExtractor) : this(options, fileSystem, normalizer, 
        tokenTransformer, directiveProcessor)
    {
        this.DocumentationExtractor = documentationExtractor;
    }

    public string BuildRoot { get; private set; } = string.Empty;
    public string BuildOutput { get; private set; } = string.Empty;

    public void Assemble(string buildFilePath)
    {
        this.SyntaxErrors = new List<ISyntaxError>();
        this.CodeLines = new List<ICodeLine>();
        this.DocLines = new List<ICodeLine>();
        this.Stack = new Stack<IStackEntry>();

        ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

// NOTE: Caller/PS Pipeline handles logic from here "down":
        // this.codelines can be serialized to a string-writer or whatever. 
        // pipeline.buildtransformer TRANSFORMS. These are just simple regex replacements vs the string/body. 
        //     no ... syntax errors or context needed at this point. (and, if so, roll this up into ... the assembler). 

        // PS pipeline checks for syntax errors. If any ... exception/terminate + report. 
        // if no errors: 
        //     REMOVE any remaining comments - regex.replace vs body/string. 
        // write artifact/body to disk. 
    }

    void IAssembler.RecursivelyAssemble(string filePath, int parentFileLine)
    {
// TODO: add a try/catch around ... all of this? 
        var buildFile = new StackEntry(filePath, parentFileLine, this.Stack.Count);
        this.Stack.Push(buildFile);

        string fileContent = this.FileSystem.GetFileContent(filePath);

        this.Normalizer.Normalize(fileContent, this.CodeLines, this.SyntaxErrors, this.Stack);

        if(this.DocumentationExtractor != null)  // TODO: ask claude about this... 
            this.DocumentationExtractor.ExtractDocumentation(this.CodeLines, this.DocLines, this.SyntaxErrors, this.Stack);

        if (this.Options.CommentRemovalDirectives.HasFlag(CommentRemovalDirectives.RemoveHeaderComments))
        {
            // TODO: use a REGEX vs .Normalizer.NormalizedText ... to identify the start/end-line of header-comments. 
            //      can, obviously be 1 (i.e., 0) or ... N. 
        }

        if(this.Options.CommentRemovalDirectives.HasFlag(CommentRemovalDirectives.RemoveDocComments))
        {
            // TODO: similar to the above ... but get a list of ALL lines with DocContent in them. ... 
            
            // TODO: Need a REGEX for DocComments that can find / address 2x scenarios: 
            //   1. ENTIRE block-comment is, effectively, a DocComment ... i.e., /* <whitespace> DOC_CONTENT <whitespace> */ ...
            //        i.e., replace the entire block-comment.
            //   2. ... there are lines with DocContent in the ... but they're NOT the only content in the block-comment itself. 
            //       just replace the matching LINES. 
        }

        this.DirectiveProcessor.ProcessDirectives(this, this.FileSystem, this.CodeLines, this.SyntaxErrors, this.Stack, this.BuildRoot, this.BuildOutput);
        
        this.TokenTransformer.TransformTokens(this.CodeLines, this.SyntaxErrors, this.Stack, this.Options.TokenDefinitionRegistry, this.Options.TokenReplacementDirectives);

        this.Stack.Pop();
    }


}