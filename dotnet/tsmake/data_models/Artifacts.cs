namespace tsmake.data_models;

public interface IArtifact
{
    string Path { get; }
    ArtifactType ArtifactType { get; }
    SourceType SourceType { get; }
    DateTime Written { get; }
}

public class BuildArtifact(string path, SourceType sourceType) : IArtifact
{   
    public string Path { get; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.Build;
    public SourceType SourceType { get; } = sourceType;
    public DateTime Written { get; private set; }
}

public class FileMarkerArtifact(string path, SourceType sourceType, IDirective sourceDirective) : IArtifact
{
    public IDirective SourceDirective { get; } = sourceDirective;
    public string Path { get; } = path;
    public ArtifactType ArtifactType { get; } = ArtifactType.FileMarker;
    public SourceType SourceType { get; } = sourceType;
    public DateTime Written { get; private set; }
}