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
        Directive = 8,
        SimpleComment = 16, 
        SimpleCommentOnly = 32,
        BlockComment = 64,
        // TODO: nested block comments
        BlockCommentStart = 128, 
        BlockCommentEnd = 256,
        BlockCommentLine = 512,
        MultipleBlockComments = 512,
        BlockCommentOnly = 1024
    }

    [Flags]
    public enum LineEndCommentType
    {
        NoComment = 0, 
        EolComment = 1, 
        WhiteSpaceAndComment = 2,
        FullLineComment = 4
    }

    [Flags]
    public enum BlockCommentType
    {
        NoComment = 0, 
        BlockComment = 1, 
        BlockCommentStart = 2, 
        BlockCommentLine = 4, 
        BlockCommentEnd = 8,
        NestedBlocComment = 16, 
        NestedBlocCommentStart = 32,
        NestedBlockCommentLine = 64,
        NestedBlocCommentEnd = 128,
        MultipleBlockComments = 256, 
        WhitespaceAndBlockComment = 512, 
        BlockCommentOnly = 1024
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