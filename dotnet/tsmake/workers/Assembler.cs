using tsmake.data_models;

namespace tsmake.workers;

public interface IAssembler
{
    List<ICodeLine> CodeLines { get; }
    List<ISyntaxError> SyntaxErrors { get; }
    List<IConfigError> ConfigErrors { get; }
    List<ICodeLine> DocLines { get; }

    // REFACTOR: pretty sure that these need to pushed down into IAssemblerOptions as they're ... just options until the build file has been looped/processed. 
    //    that said... by LEAVING these here ... it's POSSIBLE to interrogate these public collections from the COMMAND-LINE | POWERSHELL. 
    //      though... arguably: the OPTIONS object MIGHT be a better way to surface these details to callers instead? (just as long as the OPTIONS object has ALL values by the time assembly is complete).
    List<IRankedString> BuildRoot { get; }
    List<IRankedString> BuildOutput { get; }
    List<IRankedString> FileMarkerPath { get; }

    void Assemble(string buildFilePath);
    internal void RecursivelyAssemble(string filePath, int parentFileLine);
}

public class Assembler(IOpsFactory opsFactory, IAssemblerOptions options, IResult buildResult) : IAssembler
{
    private int _fileCount = 0;
    private int _directivesCount = 0;
    private bool _buildFileHandled = false;

    private IOpsFactory OpsFactory { get; } = opsFactory;

    // TODO: options is (i think) already tracking a handful of IRankedStrings for OUTPUT and ROOT (and FILEMARKER) ... 
    //      but I'm NOT copying them into the IAssembler's copy of these ... I should be ... 
    //        either that ... or I ONLY use the values in IAssemblerOptions ... and not have these in IAssembler at all.
    private IAssemblerOptions Options { get; } = options;
    private IResult BuildResult { get; } = buildResult;

    public List<ISyntaxError> SyntaxErrors { get; private set; } = new List<ISyntaxError>();
    public List<IConfigError> ConfigErrors { get; private set; } = new List<IConfigError>();
    public List<ICodeLine> CodeLines { get; private set; } = new List<ICodeLine>();
    public List<ICodeLine> DocLines { get; private set; } = new List<ICodeLine>();
    private Stack<IStackEntry> Stack { get; set; } = new Stack<IStackEntry>();

    public List<IRankedString> BuildRoot { get; private set; } = options.BuildRoot;
    public List<IRankedString> BuildOutput { get; private set; } = options.BuildOutput;
    public List<IRankedString> FileMarkerPath { get; private set; } = options.FileMarkerPath;

    public void Assemble(string buildFilePath)
    {
        try
        {
            ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

            // TODO: serialize this.codelines via a StringBuilder... 
            //      skipping any ##COMMENTS ... or ##ROOT or ##OUTPUT lines. 
            //      and using CodeLine.TransformedContent unless empty - in which case ... use CodeLine.OriginalContent.

            // IF there are any remaining CommentRemovalDirectives (removeAll, removeEOL, etc.) ... add 'stock'/built-in BuildTransforms to remove them. 

            // IF there are any <BuildTransforms> (including comment-removal transforms)... 
            //      then execute each transform in <BuildTransforms> in order ... passing the serialized-string into each one ...

            // TODO:  ARTIFACTS will be different based on .Document or .Build ... 
            //    IF .Build: 
            //         we have 1-2 artifacts: the ##output and an optional ##filemarker (version-marker) file.
            //    IF .Document:
            //        we'll have 1 artifact PER each DocTransformer/Writer. 

            this.BuildResult.SetBuildRoot(this.OpsFactory.CurrentFileSystem.RootDirectory, this.OpsFactory.CurrentFileSystem.RootSourceType);
            this.BuildResult.SetConfigErrors(this.ConfigErrors);
            this.BuildResult.SetSyntaxErrors(this.SyntaxErrors);
            this.BuildResult.SetStatistics(this._fileCount, this.CodeLines.Count, this._directivesCount);
            this.BuildResult.SetComplete();

            this.BuildResult.AddAssembler(this);
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);  // TODO: identify this as an exception within ... general assembly vs recursiveAssembly... (ditto down in .RecursivelyAssemble() below)
        }
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
                    // then remove those lines from this.CodeLines ... via a .RemoveRange() or something similar.
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
            directivesProcessor.IdentifyDirectives(this, this.OpsFactory.CurrentFileSystem, this.CodeLines, this.SyntaxErrors);

            if (!this._buildFileHandled)
            {
                // 1. ##ROOT. We'll ALWAYS have a ROOT-PATH value - even if it's PWD. 
                IRankedString buildRoot = RankedString.GetRankedValue(this.BuildRoot);
                if(buildRoot.Value.IsWhiteSpace())
                    this.ConfigErrors.Add(new ConfigError($"No Root-Path specified.", $"SOURCE: {buildRoot.SourceType}", this.BuildRoot));
                else
                {
                    if (this.OpsFactory.CurrentFileSystem.DirectoryExists(buildRoot.Value))
                        this.OpsFactory.CurrentFileSystem.SetRootDirectory(buildRoot.Value, buildRoot.SourceType);  
                    else
                        this.ConfigErrors.Add(new ConfigError($"Specified Root Path NOT Found: [{buildRoot.Value}].", $"SOURCE: {buildRoot.SourceType}", this.BuildRoot));
                }

                // 2. ##OUTPUT. We MIGHT not have an OUTPUT path. If so, we can't add an ARTIFACT for output, and need to signify this with a ConfigError.
                IRankedString buildOutput = RankedString.GetRankedValue(this.BuildOutput); 
                if (this.OpsFactory.CurrentFileSystem.DirectoryExists(buildOutput.Value))
                {
                    // create a new artifact with the buildOutput as the path. 
                    // no contents yet... but ... yeah. 
                }
                else
                    this.ConfigErrors.Add(new ConfigError($"Invalid output path specified: [{buildOutput.Value}]", $"SOURCE: {buildOutput.SourceType}", this.BuildOutput));

                // 3. ##FILEMARKER. Optional. If we have one, and the path is valid, add a new artifact. 
                IRankedString fileMarkerOutput = RankedString.GetRankedValue(this.FileMarkerPath);
                if(this.OpsFactory.CurrentFileSystem.DirectoryExists(fileMarkerOutput.Value))
                {
                    // create a new artifact with the fileMarkerOutput as the path. 
                }
                else
                    // DITTO ... as per ROOT ... need to know where this came from ...
                    this.ConfigErrors.Add(new ConfigError($"Invalid file marker path specified: {fileMarkerOutput.Value}", $"SOURCE: {fileMarkerOutput.SourceType}", this.FileMarkerPath));

                this._buildFileHandled = true;
            }

            directivesProcessor.ProcessDirectives(this, this.OpsFactory.CurrentFileSystem, this.CodeLines, this.SyntaxErrors);

            var tokenTransformer = this.OpsFactory.NewTokenTransformer();
            tokenTransformer.TransformTokens(this.CodeLines, this.SyntaxErrors, this.Stack, this.Options.TokenDefinitionRegistry, this.Options.TokenExclusionDirectives);

            this._directivesCount += directivesProcessor.Directives.Count;
            this.Stack.Pop();
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);
        }
    }
}