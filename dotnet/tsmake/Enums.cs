namespace tsmake;

public enum LineEndingOptions
{
    CrLf,
    Lf,
    Cr          
}

public enum OperationType
{
    Build,
    Document,
    Generate,
    Deploy
}

public enum ArtifactType
{
    Build,
    FileMarker,
    Documentation,
    RunnerLog,      // i.e., the log generated along side an execution 'run' for deployment/etc. 
    Generator       // output of a generation operation (e.g., against a git tag/etc.)
}

[Flags]
public enum CommentRemovalDirectives
{
    None = 0,
    RemoveHeaderComments = 1,
    RemoveDocComments = 2,
    RemoveLineEndComments = 4,
    RemoveBlockComments = 8,
    RemoveAllComments = 15  // redundant, but makes it easier to read in code.
}

[Flags]
public enum TokenReplacementDirectives
{
    None = 0,
    ExcludeStrings = 1, 
    ExcludeEolComments = 2,
    ExcludeBlockComments = 4
}

[Flags]
public enum GoHandlerDirectives
{
    None = 0, 
    RemoveGoOnlyBatches = 1,
    RemoveUseOnlyBatches = 2
}

public enum PathType
{
    NotSet,
    Absolute,
    Relative,
    Rooted
}

//public enum OrderBy
//{
//    Alphabetical,
//    ModifyDate,
//    CreateDate
//}

//public enum Direction
//{
//    Ascending,
//    Descending
//}