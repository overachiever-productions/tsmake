using tsmake.data_models;

namespace tsmake.workers;

public interface IAssembler
{
    List<ICodeLine> ArtifactLines { get; }
    string ArtifactBody { get; }
    List<ISyntaxError> SyntaxErrors { get; }
    List<IConfigError> ConfigErrors { get; }
    List<ICodeLine> DocLines { get; }

    // REFACTOR: pretty sure that these need to pushed down into IAssemblerOptions as they're ... just options until the build file has been looped/processed. 
    //    that said... by LEAVING these here ... it's POSSIBLE to interrogate these public collections from the COMMAND-LINE | POWERSHELL. 
    //      though... arguably: the OPTIONS object MIGHT be a better way to surface these details to callers instead? (just as long as the OPTIONS object has ALL values by the time assembly is complete).
    List<IRankedString> BuildRoot { get; }
    List<IRankedString> BuildOutput { get; }
    List<IRankedString> FileMarkerPath { get; }

    IAssemblerOptions Options { get; }

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
    public IAssemblerOptions Options { get; } = options;
    private IResult BuildResult { get; } = buildResult;

    public List<ISyntaxError> SyntaxErrors { get; private set; } = new List<ISyntaxError>();
    public List<IConfigError> ConfigErrors { get; private set; } = new List<IConfigError>();
    public List<ICodeLine> ArtifactLines { get; private set; } = new List<ICodeLine>();
    public string ArtifactBody { get; private set; } = string.Empty;
    public List<IArtifact> Artifacts { get; private set; } = new List<IArtifact>();

    public List<ICodeLine> DocLines { get; private set; } = new List<ICodeLine>();

    public List<IRankedString> BuildRoot { get; private set; } = options.BuildRoot;
    public List<IRankedString> BuildOutput { get; private set; } = options.BuildOutput;
    public List<IRankedString> FileMarkerPath { get; private set; } = options.FileMarkerPath;

    public void Assemble(string buildFilePath)
    {
        try
        {
            this.ExtractBuildDirectives(buildFilePath);

            var tokenTransformer = this.OpsFactory.NewTokenTransformer(this.Options.TokenExclusionDirectives, this.Options.TokenDefinitionRegistry);
            tokenTransformer.TransformBuildArtifacts(this.Artifacts, this.SyntaxErrors);

            if (this.SyntaxErrors.Count > 0)
            {
                this.MarshalResults();
                return;
            }

            ((IAssembler)this).RecursivelyAssemble(buildFilePath, 0);

            tokenTransformer.TransformCodeLines(this.ArtifactLines, this.SyntaxErrors);

            // NOTE: this.Artifacts - NOT this.BuildResult.Artifacts (which only gets them in MarshalResults()).
            if (!this.Artifacts.Any(artifact => artifact is BuildArtifact))
            {
                this.ConfigErrors.Add(new ConfigError("No BUILD Output was specified.", "Build Can't proceed..", this.BuildOutput));
                this.MarshalResults();
                return;
            }
                
            this.ArtifactBody = this.ArtifactLines.Serialize(this.Options.LineEndingsType);
            this.ArtifactBody = CommentRemover.RemoveComments(this.ArtifactBody, this.Options.CommentRemovalDirectives);

            //   https://overachieverllc.atlassian.net/browse/TSM-46
            // TODO: Process <BuildTransforms> if any - in order of declaration (as passed in by the caller). 
            //       Include 'options'/transforms for remaining comment-removal directives (e.g., RemoveAll, RemoveEOL, etc.) 
            //       Include 'batch/GO' handler directives if they were passed in by the caller.
            //       Then handle any user-defined transforms.  
            //      Each transform is, essentially, a (semi-complex) REGEX + REPLACE - against the entire 'body' of the main output/artifact. 
            //          MAIN-ARTIFACT = this.ArtifactBody if BUILD or a ... something-something representation of ALL documentation components (if we're in DOCUMENT mode).

            foreach (IArtifact artifact in this.Artifacts)
            {
                try
                {
                    Artifactory.PopulateArtifact(this, artifact);
                    artifact.Persist(this.OpsFactory.CurrentFileSystem);
                }
                catch (Exception ex)
                {
                    this.BuildResult.AddException(ex);
                }
            }

            this.MarshalResults();
        }
        catch (Exception ex)
        {
            // TODO: identify this as an exception within ... general assembly vs recursiveAssembly... (ditto down in .RecursivelyAssemble() below)
            this.BuildResult.AddException(ex);  
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
                    RemoveHeaderComments(localCodeLines, normalizer.NormalizedText);
            }

            // MKC: Everything done within directiveProcessor.ProcessDirectives COULD 'easily' be done here - inline - with a bunch of additional code blocks. 
            //      BUT, the point of the DirectiveProcessor is to ISOLATE logic to better enable Unit-Testing, etc. 
            var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();  
            directivesProcessor.ProcessDirectives(this, this.OpsFactory.CurrentFileSystem, localCodeLines, this.ArtifactLines, this.SyntaxErrors);
            
            this._directivesCount += directivesProcessor.Directives.Count;
            this._depth--;
        }
        catch (Exception ex)
        {
            this.BuildResult.AddException(ex);
        }
    }

    private void MarshalResults()
    {
        this.BuildResult.SetBuildRoot(this.OpsFactory.CurrentFileSystem.RootDirectory, this.OpsFactory.CurrentFileSystem.RootSourceType);
        this.BuildResult.SetFlagsDetails(this.Options.Flags, this.Options.FlagsSource);
        this.BuildResult.SetConfigErrors(this.ConfigErrors);
        this.BuildResult.SetSyntaxErrors(this.SyntaxErrors);
        this.BuildResult.SetStatistics(this._fileCount, this.ArtifactLines.Count, this._directivesCount);
        this.BuildResult.Artifacts.AddRange(this.Artifacts);

        this.BuildResult.SetComplete();

        this.BuildResult.AddAssembler(this);
    }

    // Removes the file's header comments (see Global.HeaderCommentsRegex) from its code-lines:
    //      - every line the header covers ENTIRELY is removed.
    //      - if a block comment ends ON the line where code starts (e.g., "*/USE [master];"), only the part of that line BEFORE the code is removed.
    //  NOTE: the regex runs against NormalizedText - which has exactly ONE line-break per code-line boundary (same as Normalize()'s split) -
    //      so N line-breaks in the header = the first N code-lines. Remaining lines keep their ORIGINAL line numbers (for error reporting).
    private static void RemoveHeaderComments(List<ICodeLine> codeLines, string normalizedText)
    {
        Match header = Global.HeaderCommentsRegex.Match(normalizedText);
        if (!header.Success || header.Length == 0)
            return;

        if (header.Length == normalizedText.Length)
        {
            codeLines.Clear();      // nothing BUT whitespace and comments in this file.
            return;
        }

        var lineBreaks = Regex.Matches(header.Value, @"\r\n|\r|\n");
        int wholeLines = lineBreaks.Count;
        int partial = wholeLines == 0 ? header.Length : header.Length - (lineBreaks[^1].Index + lineBreaks[^1].Length);

        codeLines.RemoveRange(0, wholeLines);

        if (partial > 0)
        {
            var codeStart = codeLines[0];
            codeLines[0] = new CodeLine(codeStart.OriginalContent.Substring(partial), codeStart.LineNumber, codeStart.StartOffset + partial, codeStart.EndOffset, codeStart.FileDetail);
        }
    }

    private void ExtractBuildDirectives(string buildFilePath)
    {
        var buildFile = new FileDetail(buildFilePath, 0, 1);
        string fileContent = this.OpsFactory.CurrentFileSystem.GetFileContent(buildFilePath);
        var normalizer = this.OpsFactory.NewNormalizer();
        var buildLines = new List<ICodeLine>();
        normalizer.Normalize(fileContent, buildLines, this.SyntaxErrors, buildFile);
        var directivesProcessor = this.OpsFactory.NewDirectiveProcessor();
        directivesProcessor.ProcessGlobalBuildFileDirectives(this, this.OpsFactory.CurrentFileSystem, buildLines);

        IRankedString buildRoot = RankedString.GetRankedValue(this.BuildRoot);
        if (buildRoot.IsValid)  // if NOT valid, a .ConfigError has already been logged. 
            this.OpsFactory.CurrentFileSystem.SetRootDirectory(buildRoot.Value, buildRoot.SourceType);

        IRankedString buildOutput = RankedString.GetRankedValue(this.BuildOutput);
        if (buildOutput.IsValid)  // if NOT valid, a .ConfigError has already been logged. 
            this.Artifacts.Add(new BuildArtifact(buildOutput.Value, buildOutput.SourceType, buildOutput.SourceDirective));

        IRankedString fileMarkerOutput = RankedString.GetRankedValue(this.FileMarkerPath);
        if (fileMarkerOutput.IsValid) // if NOT valid, a .ConfigError has already been logged. 
        {
            var fileMarker = new FileMarkerArtifact(fileMarkerOutput.Value, fileMarkerOutput.SourceType, fileMarkerOutput.SourceDirective);

            // HACK: temporary implementation. See https://overachieverllc.atlassian.net/browse/TSM-48 for more info. 
            //    I'll LIKELY allow the DirectivesParser to have a TokenTransformer - so that OUTPUT and FILE-MARKER directives can be token-processed during LOAD/DISCOVERY. 
            fileMarker.TranslatePlaceHolders(this.Options, this.OpsFactory.CurrentFileSystem, this.SyntaxErrors);

            this.Artifacts.Add(fileMarker);
        }
            
    }
}