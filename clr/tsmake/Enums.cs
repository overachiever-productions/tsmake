namespace tsmake
{
    public enum VersionScheme
    {
        FourPart,
        Semantic,
        Custom
    }

    public enum PathType
    {
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

    public enum ProcessingType
    {
        BuildFile, 
        IncludedFile
    }

    public enum ProcessingOutcome
    {
        Failure, 
        Success
    }

    public enum CommentRemovalOption
    {
        None, // don't remove any comments. 
        FirstHeaderBlockComment, // only remove the first header comment. 
        AllHeaderBlockComments, 
        AllBlockComments, 
        AllComments // block, header, everything. NOT sure I'm going to be able to tackle this but... it's worth a shot. 
    }

    public enum OperationType
    {
        Docs,
        Build,
        DocsAndBuild, 
        Runner, 
        Generator
    }

    public enum RecursionOption
    {
        Recurse,
        TopOnly
    }
}