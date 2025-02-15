namespace tsmake;

public enum VersionScheme
{
    FourPart,
    Semantic,
    Custom
}

public enum PathType
{
    NotSet,
    Absolute,
    Relative,
    Rooted
}

public enum OrderBy
{
    Alphabetical,
    ModifyDate,
    CreateDate
}

public enum Direction
{
    Ascending,
    Descending
}

[Flags]
public enum CommentType
{
    None = 1,
    LineEndComment = 2,
    BlockComment = 4
}

public enum ErrorType
{
    Runtime,            
    Configuration, 
    Validation,
    Syntax,
    Build, 
    Documentation
}

public enum ArtifactType
{
    Build, 
    FileMarker, 
    Documentation, 
    RunnerLog,      // i.e., the log generated along side an execution 'run' for deployment/etc. 
    Generator       // output of a generation operation (e.g., against a git tag/etc.)
}

public enum Verb
{
    Build, 
    Docs, 
    BuildAndDocs, 
    Generate, 
    Run
}

// TODO: these are WAAAAY too close in scope/value to Verbs... 
public enum OperationType
{
    Build,
    Docs,
    BuildAndDocs,
    Runner,
    Generator
}