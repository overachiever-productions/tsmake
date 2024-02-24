namespace tsmake
{
    public enum VersionScheme
    {
        FourPart,
        Semantic,
        Organic
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
    public enum LineType
    {
        RawContent = 1,
        WhitespaceOnly = 2,
        TokenizedContent = 4,
        Directive = 8
    }

    public enum ErrorSeverity
    {
        Fatal,
        // might need something in the middle - between Fatal/Warning... 
        Warning
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
}