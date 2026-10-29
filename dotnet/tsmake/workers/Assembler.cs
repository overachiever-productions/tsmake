using tsmake.data_models;

namespace tsmake.workers;

public interface IAssembler
{
    List<ICodeLine> ArtifactLines { get; }
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
    private int _depth = 0;
    private int _fileCount = 0;
    private int _directivesCount = 0;

    private IOpsFactory OpsFactory { get; } = opsFactory;

    // TODO: options is (i think) already tracking a handful of IRankedStrings for OUTPUT and ROOT (and FILEMARKER) ... 
    //      but I'm NOT copying them into the IAssembler's copy of these ... I should be ... 
    //        either that ... or I ONLY use the values in IAssemblerOptions ... and not have these in IAssembler at all.
    private IAssemblerOptions Options { get; } = options;
    private IResult BuildResult { get; } = buildResult;

    public List<ISyntaxError> SyntaxErrors { get; private set; } = new List<ISyntaxError>();
    public List<IConfigError> ConfigErrors { get; private set; } = new List<IConfigError>();
    public List<ICodeLine> ArtifactLines { get; private set; } = new List<ICodeLine>();
    public List<ICodeLine> DocLines { get; private set; } = new List<ICodeLine>();

    public List<IRankedString> BuildRoot { get; private set; } = options.BuildRoot;
    public List<IRankedString> BuildOutput { get; private set; } = options.BuildOutput;
    public List<IRankedString> FileMarkerPath { get; private set; } = options.FileMarkerPath;

    public void Assemble(string buildFilePath)
    {
        try
        {
            this.ValidateBuildFileAndExtractBuildDirectives(buildFilePath);

            ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

    // MKC - NOTE: Any syntax errors or other problems that are found from this point forward ... 
    //          need to pull file/source information from the [ArtifactLines] collection - which has this info - per assembled code-line. 
            var tokenTransformer = this.OpsFactory.NewTokenTransformer();
            tokenTransformer.TransformTokens(this.ArtifactLines, this.SyntaxErrors, this.Options.TokenDefinitionRegistry, this.Options.TokenExclusionDirectives);

            // TODO: Significant Transition Happens Here. 
            //     until this point, everything has been 'line-based' - where we've: 
            //          - extracted root/output/filemarker directives, etc. 
            //          - recursively assembled all artifacts (including includes, conditionals, etc.)
            //          - handled all CONDITIONALs (including conditional-files > converted to FILE|DIRECTORY then processed with all other FILE|DIRECTORY directives). 
            //          - and replaced all tokens (via TokenTransformer.TransformTokens()) ...
            //    FROM here forward, we'll be working against a SERIALIZED copy of ArtifactLines<> ... 
            //          i.e., at text/file-string level - effectively all via REGEX at this point. 

            // IF there are any <BuildTransforms> (including comment-removal transforms)...
            //      then execute each transform in <BuildTransforms> in order ... passing the serialized-string into each one ...
            // NOTE: CommentRemovalDirectives like RemoveAll, RemoveEOL, and a few other 'global' directives WILL be handled as 
            //       internal/built-in <BuildTransforms> - i.e., if we need to process any Comment-Removal Directives we'll: 
            //       a) add them as transforms - essentially: regex to match the comment-type and ... empty-string as the replacement. 
            //       b) then we'll process THOSE before any other transforms (supplied by end-users/callers). 

            // TODO:  ARTIFACTS will be different based on .Document or .Build ... 
            //    IF .Build: 
            //         we have 1-2 artifacts: the ##output and an optional ##filemarker (version-marker) file.
            //    IF .Document:
            //        we'll have 1 artifact PER each DocTransformer/Writer. 

            this.BuildResult.SetBuildRoot(this.OpsFactory.CurrentFileSystem.RootDirectory, this.OpsFactory.CurrentFileSystem.RootSourceType);
            this.BuildResult.SetConfigErrors(this.ConfigErrors);
            this.BuildResult.SetSyntaxErrors(this.SyntaxErrors);
            this.BuildResult.SetStatistics(this._fileCount, this.ArtifactLines.Count, this._directivesCount);
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
            this._depth++;
            this._fileCount++;

            var codeFile = new FileDetail(filePath, parentFileLine, this._depth);
            string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(filePath);

            var normalizer = this.OpsFactory.NewNormalizer();    
            var localCodeLines = new List<ICodeLine>();  // REFACTOR: doesn't REALLY matter. But stop passing these by REF. Normalizer.Normalize() should return a List<ICodeLine>. 
            normalizer.Normalize(fileContent, localCodeLines, this.SyntaxErrors, codeFile);  // normalizer DOES need syntaxErrors and stack - by reference. 

            if (this.Options.OperationType == OperationType.Document)
            {
                var docExtractor = this.OpsFactory.NewDocumentationExtractor();
                docExtractor.ExtractDocumentation(localCodeLines, this.DocLines, this.SyntaxErrors, codeFile);
            }
            else
            {
                if (this.Options.CommentRemovalDirectives.HasFlag(CommentRemovalDirectives.RemoveHeaderComments))
                {
                    // TODO: use a REGEX vs .Normalizer.NormalizedText ... to identify the start/end-line of header-comments. 
                    //      can, obviously be 1 (i.e., 0) or ... N. 
                    // then remove those lines from normalizedCodeLines ... via a .RemoveRange() or something similar.
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

            // MKC: Everything done within directiveProcessor.ProcessDirectives COULD 'easily' be done here - inline - with a bunch of additional code blocks. 
            //      BUT, the point of the DirectiveProcessor is to ISOLATE logic to better enable Unit-Testing, etc. 
            var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();  // Hmmm. Factory should inject NEW DirectivesProcessor() with an IFileSystem. THOUGH ... that does make things a BIT harder to unit test... 
            directivesProcessor.ProcessDirectives(this, this.OpsFactory.CurrentFileSystem, localCodeLines, this.ArtifactLines, this.SyntaxErrors);
            
            this._directivesCount += directivesProcessor.Directives.Count;
            this._depth--;
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);
        }
    }

    private void ValidateBuildFileAndExtractBuildDirectives(string buildFilePath)
    {
        var buildFile = new FileDetail(buildFilePath, 0, 1);
        string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(buildFilePath);
        var normalizer = this.OpsFactory.NewNormalizer();
        var buildLines = new List<ICodeLine>();
        normalizer.Normalize(fileContent, buildLines, this.SyntaxErrors, buildFile);
        var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();
        directivesProcessor.ProcessGlobalBuildFileDirectives(this, this.OpsFactory.CurrentFileSystem, buildLines);

        IRankedString buildRoot = RankedString.GetRankedValue(this.BuildRoot);
        // TODO: should NOT BE EMPTY.
        if (buildRoot.IsValid)  // if NOT valid, a .ConfigError has already been logged. 
            this.OpsFactory.CurrentFileSystem.SetRootDirectory(buildRoot.Value, buildRoot.SourceType);

        IRankedString buildOutput = RankedString.GetRankedValue(this.BuildOutput);
        // TODO: might be empty... 
        if (buildOutput.IsValid)  // if NOT valid, a .ConfigError has already been logged. 
            this.BuildResult.Artifacts.Add(new BuildArtifact(buildOutput.Value, buildOutput.SourceType));

        IRankedString fileMarkerOutput = RankedString.GetRankedValue(this.FileMarkerPath);
        // TODO: might be empty ... 
        if (fileMarkerOutput.IsValid)  // if NOT valid, a .ConfigError has already been logged. 
            this.BuildResult.Artifacts.Add(new FileMarkerArtifact(fileMarkerOutput.Value, fileMarkerOutput.SourceType, fileMarkerOutput.SourceDirective));
    }
}