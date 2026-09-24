using tsmake.data_models;

namespace tsmake.workers;

public interface IAssembler
{
    string BuildRoot { get; }
    string BuildOutput { get; }
    void Assemble(string buildFilePath);

    internal void RecursivelyAssemble(string filePath, int parentFileLine);
}

public class Assembler(IOpsFactory opsFactory, IAssemblerOptions options, IResult buildResult) : IAssembler
{
    private int _fileCount = 0;

    private IOpsFactory OpsFactory { get; } = opsFactory;
    private IAssemblerOptions Options { get; } = options;
    private IResult BuildResult { get; } = buildResult;

    private List<ISyntaxError> SyntaxErrors { get; set; } = new List<ISyntaxError>();
    private List<ICodeLine> CodeLines { get; set; } = new List<ICodeLine>();
    private List<ICodeLine> DocLines { get; set; } = new List<ICodeLine>();
    private Stack<IStackEntry> Stack { get; set; } = new Stack<IStackEntry>();

    public string BuildRoot { get; private set; } = string.Empty;
    public string BuildOutput { get; private set; } = string.Empty;

    public void Assemble(string buildFilePath)
    {
        this.SyntaxErrors = new List<ISyntaxError>();
        this.CodeLines = new List<ICodeLine>();
        this.DocLines = new List<ICodeLine>();
        this.Stack = new Stack<IStackEntry>();

        ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

        // TODO: serialize this.codelines via a StringBuilder... 

        // TODO: pass the serialized-string into a new IBuildTransformer. 
        //    and have it run transforms ... (which can/will include options for replacing UseONlyBatches, EmptyBatches, etc.)

        // TODO: the IBuildTransformer can/will ALSO address any 'remaining' CommentRemovalDirectives. 
        //      i.e., those are 'just' REGEX replaces as well. 

        // TODO:  ARTIFACTS will be different based on .Document or .Build ... 
        //    IF .Build: 
        //         we have 1-2 artifacts: the ##output and an optional ##filemarker (version-marker) file.
        //    IF .Document:
        //        we'll have 1 artifact PER each DocTransformer/Writer. 

        this.BuildResult.SetErrors(this.SyntaxErrors);
        this.BuildResult.SetStatistics(this._fileCount, this.CodeLines.Count);
        this.BuildResult.SetComplete();
    }

    void IAssembler.RecursivelyAssemble(string filePath, int parentFileLine)
    {
        try
        {
            var buildFile = new StackEntry(filePath, parentFileLine, this.Stack.Count);
            this.Stack.Push(buildFile);
            this._fileCount++;

            string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(filePath);

            var normalizer = this.OpsFactory.NewNormalizer();
            normalizer.Normalize(fileContent, this.CodeLines, this.SyntaxErrors, this.Stack);

            if (this.Options.OperationType == OperationType.Document)
            {
                var docExtractor = this.OpsFactory.NewDocumentationExtractor();
                docExtractor.ExtractDocumentation(this.CodeLines, this.DocLines, this.SyntaxErrors, this.Stack);
            }
            else
            {
                if (this.Options.CommentRemovalDirectives.HasFlag(CommentRemovalDirectives.RemoveHeaderComments))
                {
                    // TODO: use a REGEX vs .Normalizer.NormalizedText ... to identify the start/end-line of header-comments. 
                    //      can, obviously be 1 (i.e., 0) or ... N. 
                }

                if (this.Options.CommentRemovalDirectives.HasFlag(CommentRemovalDirectives.RemoveDocComments))
                {
                    // TODO: similar to the above ... but get a list of ALL lines with DocContent in them. ... 

                    // TODO: Need a REGEX for DocComments that can find / address 2x scenarios: 
                    //   1. ENTIRE block-comment is, effectively, a DocComment ... i.e., /* <whitespace> DOC_CONTENT <whitespace> */ ...
                    //        i.e., replace the entire block-comment.
                    //   2. ... there are lines with DocContent in the ... but they're NOT the only content in the block-comment itself. 
                    //       just replace the matching LINES. 
                }
            }

            var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();
            directivesProcessor.ProcessDirectives(this, this.OpsFactory.CurrentFileSystem, this.CodeLines, this.SyntaxErrors, this.Stack, this.BuildRoot, this.BuildOutput);

        // TODO: at this point we NEED to have the ##output (##root was already needed within .ProcessDirectives() ... to resolve any relative paths).
        // TODO: we should also have/know if we have a ##filemarker(version-marker) or whatever as well. 

            var tokenTransformer = this.OpsFactory.NewTokenTransformer();
            tokenTransformer.TransformTokens(this.CodeLines, this.SyntaxErrors, this.Stack, this.Options.TokenDefinitionRegistry, this.Options.TokenExclusionDirectives);

            this.Stack.Pop();
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);
        }
    }
}