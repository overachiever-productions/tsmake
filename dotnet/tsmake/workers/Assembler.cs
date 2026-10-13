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
    private Stack<IStackEntry> Stack { get; set; } = new Stack<IStackEntry>();

    public List<IRankedString> BuildRoot { get; private set; } = options.BuildRoot;
    public List<IRankedString> BuildOutput { get; private set; } = options.BuildOutput;
    public List<IRankedString> FileMarkerPath { get; private set; } = options.FileMarkerPath;

    public void Assemble(string buildFilePath)
    {
        try
        {
            #region REFACTOR
            // REFACTOR: All logic in this block is a HACK to get ##ROOT, ##OUTPUT, and ##FILEMARKER BEFORE we jump into recursively assembling artifacts/output. 
            //   also ... I do NOT need ##OUTPUT or ##FILEMARKER if we're doing a DOCUMENT build ... but I do need ##ROOT for INCLUDES. 

            // REFACTOR: The logic below is clunky - because of how it was originally isolated/inverted to be solely processed in the IDirectivesProcessor 
            //     against MEMBERS of the IAssember (i.e., the List<ICodeLine>). But this complexity is NO LONGER NEEDED. 
            //   ergo: 
            // REFACTOR IDirectivesProcessor.ProcessGlobalBuildFileDirectives() to: 
            //     a) rename it ... it's no longer just identifying directives ... it's ... getting/setting ROOT/OUTPUT/FILEMARKER ... 
            //     b) don't pass in List<ICodeLine> ... instead, pass in the NormalizedText and run a REGEX. 
            //     c) and for each match ... 
            //            convert to an IDirective (via regex text)... 
            //             and if valid/legal ... set parent.xxxxRoot.Add(new rankedString()) ... accordingly. 
            //     d) if invalid ... add a ConfigError to parent.ConfigErrors.
            //            invalid could/would be: SyntaxError: missing or invalid syntax/etc. 
            //                 or a CONFIG error: duplicate directive, file-not-found / etc. 

            string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(buildFilePath);
            var normalizer = this.OpsFactory.NewNormalizer();
            var buildLines = new List<ICodeLine>();
            normalizer.Normalize(fileContent, buildLines, this.SyntaxErrors, this.Stack);

            var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();
            directivesProcessor.ProcessGlobalBuildFileDirectives(this, this.OpsFactory.CurrentFileSystem, buildLines, this.SyntaxErrors);

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
            #endregion 

            ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

            var tokenTransformer = this.OpsFactory.NewTokenTransformer();
            tokenTransformer.TransformTokens(this.ArtifactLines, this.SyntaxErrors, this.Stack, this.Options.TokenDefinitionRegistry, this.Options.TokenExclusionDirectives);

            // TODO: Now that we have a fully 'recursed' and assembled list of lines with conditionals and includes,
            //      AND we've replaced all tokens ... serialize this.codelines via a StringBuilder... cuz we're only doing 'string' transforms (via REGEX)
            //      at this point instead of line-by-line operations. 

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
            var codeFile = new StackEntry(filePath, parentFileLine, this.Stack.Count);
            this.Stack.Push(codeFile);
            this._fileCount++;

            string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(filePath);

            var normalizer = this.OpsFactory.NewNormalizer();    
            var localCodeLines = new List<ICodeLine>();  // REFACTOR: doesn't REALLY matter. But stop passing these by REF. Normalizer.Normalize() should return a List<ICodeLine>. 
            normalizer.Normalize(fileContent, localCodeLines, this.SyntaxErrors, this.Stack);  // normalizer DOES need syntaxErrors and stack - by reference. 

            if (this.Options.OperationType == OperationType.Document)
            {
                var docExtractor = this.OpsFactory.NewDocumentationExtractor();
                docExtractor.ExtractDocumentation(localCodeLines, this.DocLines, this.SyntaxErrors, this.Stack);
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

// MKC: the ENTIRE point of a directivesProcessor is that it provides a UoW that can be easily unit-tested for directives identification and ... processing. 
//     the RUB is that it adds a bit of complexity (well, a decent amount really - because of recursion). 
//     but ... if you think of directivesProcessor.ProcessDirectives() as logic/code that'd OTHERWISE be in a big old "block of code" inside this method instead of being 
//          in an 'outside' class/func ... then ... stuff is a bit more logical/obvious... 

            var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();  // Hmmm. Factory should inject NEW DirectivesProcessor() with an IFileSystem. THOUGH ... that does make things a BIT harder to unit test... 
            directivesProcessor.ProcessDirectives(this, this.OpsFactory.CurrentFileSystem, localCodeLines, this.ArtifactLines, this.SyntaxErrors);

            this._directivesCount += directivesProcessor.Directives.Count;
            this.Stack.Pop();
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);
        }
    }
}