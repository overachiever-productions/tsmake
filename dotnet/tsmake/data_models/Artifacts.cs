namespace tsmake.data_models;

public interface IArtifact
{
    string Path { get; }
    ArtifactType ArtifactType { get; }
    DateTime Written { get; }
}