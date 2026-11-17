using tsmake.workers;

namespace tsmake.data_models;

public interface IArtifact
{
    string Path { get; }
    ArtifactType ArtifactType { get; }
    SourceType SourceType { get; }
    IFileWriteResult WriteResult { get; set; }
    void Persist(IFileSystem fileSystem);
}

public class BuildArtifact(string path, SourceType sourceType) : IArtifact
{   
    public string Path { get; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.Build;
    public SourceType SourceType { get; } = sourceType;
    public IFileWriteResult WriteResult { get; set; } = null!;
    public string ArtifactBody { get; private set; } = string.Empty;

    internal void SetBody(string artifactBody)
    {
        this.ArtifactBody = artifactBody;
    }

    public void Persist(IFileSystem fileSystem)
    {
        this.WriteResult = fileSystem.WriteFile(this.Path, this.ArtifactBody);
    }
}

public class FileMarkerArtifact(string path, SourceType sourceType, IDirective sourceDirective) : IArtifact
{
    public IDirective SourceDirective { get; } = sourceDirective;
    public string Path { get; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.FileMarker;
    public SourceType SourceType { get; } = sourceType;
    public IFileWriteResult WriteResult { get; set; } = null!;

    public void Persist(IFileSystem fileSystem)
    {

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