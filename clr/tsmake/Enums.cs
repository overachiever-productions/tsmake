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
    public enum LineType
    {
        RawContent = 1, 
        WhiteSpaceOnly = 2,
        ContainsTokens = 4, 
        Directive = 8, 
        ContainsStrings = 16, 
        ContainsComments = 32
    }

    [Flags]
    public enum CommentType
    {
        None = 1, 
        LineEndComment = 2, 
        BlockComment = 4
    }

    [Flags]
    public enum LineEndCommentType
    {
        EolComment = 1, 
        WhiteSpaceAndComment = 2,
        FullLineComment = 4
    }

    [Flags]
    public enum BlockCommentType
    {
        SingleLine = 1,
        MultiLineStart = 2,
        MultilineLine = 4, 
        MultilineEnd = 8,
        CommentOnly = 16        // i.e., nothing other than just a comment (usually for multi-line comments - but could be /* single line too */
    }

    [Flags]
    public enum StringType
    {
        SingleLine = 1,
        MultiLine = 2,
        MultiLineStart = 4,
        MultiLineLine = 8,
        MultiLineEnd = 16
    }

    //[Flags]
    //public enum MultiLineType
    //{
    //    None = 0,
    //    MultiLineStart = 1, 
    //    MultiLineLine = 2,
    //    MultiLineEnd = 4
    //}

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