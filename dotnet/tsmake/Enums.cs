namespace tsmake;

public enum LineEndingsType
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

public enum FlagsSource
{
    NotSet,
    Console, 
    Config
}

public enum SourceType
{
    None,
    CommandLine, // 1 
    ConfigFile,  // 2
    BuildFile,     // 3
    Convention   // 4  (e.g., default values like PWD, etc.)  
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
    NotSet = 0,
    RemoveNone = 1,
    RemoveHeaderComments = 2, RemoveHeader = RemoveHeaderComments,
    RemoveDocComments = 4, RemoveDoc = RemoveDocComments,
    RemoveLineEndComments = 8, RemoveEol = RemoveLineEndComments,
    RemoveBlockComments = 16, RemoveBlock = RemoveBlockComments,
    RemoveAllComments = 30, RemoveAll = RemoveAllComments       // redundant - but makes it easier to read in co
}

[Flags]
public enum TokenExclusionDirectives
{
    None = 0,
    ExcludeStrings = 1, 
    ExcludeEolComments = 2,
    ExcludeBlockComments = 4
}

//[Flags]
//public enum GoHandlerDirectives
//{
//    RemoveNone = 0, 
//    RemoveGoOnlyBatches = 1,
//    RemoveUseOnlyBatches = 2
//}

public enum PathType
{
    NotSet,
    Absolute,
    Relative,
    Rooted
}

public enum DirectiveType
{
    Root,
    Output,
    Runner,
    FileMarker,
    VersionChecker,
    Comment,
    File,
    Directory,
    FlagFile,
    FlagDirectory,
    ConditionDefault,
    Condition,
    ConditionEnd,
    Invalid         // a line that LOOKS like a directive but isn't a valid one - e.g., ##FLAG(#Azure): ... (see InvalidDirective).
}

public enum DirectorySortMarker
{
    None,   // default - if none specified. However, DEFAULT behavior is Alpha.
    Alpha,
    AlphaDesc,
    Timestamp,
    TimestampDesc
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