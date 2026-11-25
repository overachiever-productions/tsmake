using tsmake.workers;

namespace tsmake.data_models;

public interface IArtifact
{
    string Path { get; }
    ArtifactType ArtifactType { get; }
    SourceType SourceType { get; }
    // The directive the artifact came from (e.g., ##OUTPUT, ##FILE-MARKER) - for its line, in errors.
    //      null: the artifact didn't come from a directive (e.g., its path was passed in by the caller).
    IDirective SourceDirective { get; }
    IFileWriteResult WriteResult { get; set; }

    // Resolves .Path - the same way as every other path: relative => the build file's folder, ~\ => ##ROOT - and checks that the
    //      directory it'll be written INTO exists (a syntax error if not - and .Path is left as-is). Called AFTER ##ROOT is set and the
    //      path's tokens are replaced (TokenTransformer.TransformBuildArtifacts) - so ~\ works, and tokens can be part of the DIRECTORY.
    void TranslatePath(IFileSystem fileSystem, List<ISyntaxError> syntaxErrors);
    void Persist(IFileSystem fileSystem);
}

public class BuildArtifact(string path, SourceType sourceType, IDirective sourceDirective) : IArtifact
{
    public string Path { get; private set; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.Build;
    public SourceType SourceType { get; } = sourceType;
    public IDirective SourceDirective { get; } = sourceDirective;
    public IFileWriteResult WriteResult { get; set; } = null!;

    public string ArtifactBody { get; private set; } = string.Empty;

    internal void SetBody(string artifactBody)
    {
        this.ArtifactBody = artifactBody;
    }

    // Used by TokenTransformer.TransformBuildArtifacts() - i.e., the path with its tokens replaced.
    internal void SetPath(string path)
    {
        this.Path = path;
    }

    public void TranslatePath(IFileSystem fileSystem, List<ISyntaxError> syntaxErrors)
    {
        if (this.ResolvePath("OUTPUT", fileSystem, syntaxErrors) is { } fullPath)
            this.Path = fullPath;
    }

    public void Persist(IFileSystem fileSystem)
    {
        this.WriteResult = fileSystem.WriteFile(this.Path, this.ArtifactBody);
    }
}

public class FileMarkerArtifact(string path, SourceType sourceType, IDirective sourceDirective) : IArtifact
{
    public string Path { get; private set; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.FileMarker;
    public SourceType SourceType { get; } = sourceType;
    public IFileWriteResult WriteResult { get; set; } = null!;

    // From the ##FILE-MARKER directive's [body: ...] clause - as-is (i.e., {placeholders}, `r`n, etc. are NOT processed yet).
    //      null: no [body: ...] clause - or the path didn't come from a directive (e.g., it was passed in by the caller).
    public string? ArtifactBody { get; private set; } = (sourceDirective as FileMarkerDirective)?.Body;
    public IDirective SourceDirective { get; } = sourceDirective;

    // Used by TokenTransformer.TransformBuildArtifacts() - i.e., the path and body with their tokens replaced.
    internal void SetPath(string path)
    {
        this.Path = path;
    }

    internal void SetBody(string? artifactBody)
    {
        this.ArtifactBody = artifactBody;
    }

    public void TranslatePath(IFileSystem fileSystem, List<ISyntaxError> syntaxErrors)
    {
        if (this.ResolvePath("FILE-MARKER", fileSystem, syntaxErrors) is { } fullPath)
            this.Path = fullPath;
    }

    public void Persist(IFileSystem fileSystem)
    {
        this.WriteResult = fileSystem.WriteFile(this.Path, this.ArtifactBody ?? "");
    }
}

public static class ArtifactExtensions
{
    // A syntax error ABOUT an artifact - reported against its directive's line (##OUTPUT, ##FILE-MARKER). No directive (e.g., the
    //      path was passed in by the caller): no line number - just where the path came from, e.g., "(CommandLine)".
    public static ISyntaxError NewSyntaxError(this IArtifact artifact, string message, string detail)
    {
        ICodeLine? codeLine = artifact.SourceDirective?.CodeLine;

        return codeLine is not null
            ? new SyntaxError(message, detail, codeLine)
            : new SyntaxError(message, detail, 0, new FileDetail($"({artifact.SourceType})", 0, 0));
    }

    // Shared by every artifact's TranslatePath() (see IArtifact): the translated path - or null if it can't be used (already reported).
    //      directiveName: for messages - e.g., "OUTPUT", "FILE-MARKER".
    internal static string? ResolvePath(this IArtifact artifact, string directiveName, IFileSystem fileSystem, List<ISyntaxError> syntaxErrors)
    {
        if (Global.TokenRegex.IsMatch(artifact.Path))
            return null;     // an UNDEFINED token - already reported by TokenTransformer.TransformBuildArtifacts().

        string fullPath = fileSystem.TranslatePath(artifact.Path);
        if (!fileSystem.DirectoryExists(fullPath))      // NOTE: checks the directory the file will be written INTO.
        {
            string directory = System.IO.Path.GetDirectoryName(fullPath) ?? fullPath;
            syntaxErrors.Add(artifact.NewSyntaxError($"Invalid --##{directiveName} Directive.", $"The directory for the specified ##{directiveName} path does not exist: {directory}"));
            return null;
        }

        return fullPath;
    }
}

// MKC: This is a BIT of a hack. It exists for 2 main reasons: 
//      1. Felt a bit vile to let the Assembler interface directly with an Artifact to 'give each artifact' what it needed. 
//          likewise, felt odd/wrong to pass a FULL-blown Assembler into an Artifact to let the Artifact 'pull' what it needed (with FULL access to the Assembler). 
//          So ... this acts as a 'gated' intermediary. Any interchange between the Assembler and an Artifact must be explicitly defined here. 
//      2. I MAY end up having some Artifacts that need MORE than what is available to the Assembler (e.g., db-connections? for documentation?) or whatever. 
//          IF (if) that ends up being the case, I can manage that mapping here (may have to change this from being a static method/etc. to manage IoC/State 
//              BUT... even if so, that's a single signature(ish)/interface to manipulate vs a bunch of code in the Assembler handling 'stuff'. 
public class Artifactory
{
    public static void PopulateArtifact(IAssembler assembler, IArtifact artifact)
    {
        // MKC: Yes. This is 100% as UGLY as it looks </shame>: 
        switch (artifact.ArtifactType)
        {
            case ArtifactType.Build:
                (artifact as BuildArtifact)?.SetBody(assembler.ArtifactBody);
                break;
            case ArtifactType.FileMarker:
                // Handle file marker artifact
                break;
            default:
                throw new NotImplementedException($"Unhandled artifact type: {artifact.ArtifactType}");
        }
    }
}