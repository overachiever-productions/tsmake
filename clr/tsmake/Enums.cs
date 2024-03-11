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
        None = 0, 
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
        None = 0, 
        LineEndComment = 1, 
        BlockComment = 2
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
        CommentOnly = 1, 
        WhiteSpaceAndComment = 2,
        EolComment = 4,
        MidlineComment = 8, 
        MultipleSingleLineComments = 16, 
        NestedSingleLineComments = 32,   // NOT sure I care about these - i.e., as long as they're something I can strip/remove/ignore as needed ... then ... who cares if they exist. 
        MultiLineStart = 64,
        MultilineLine = 128, 
        MultilineEnd = 256,
        MultiLineNested = 512           // might also NOT care about this ... only, what happens if i'm in the middle of .ismultiLine and find a new /* ... then... i think that'd 'bump' this enum up, right? 
    }

    [Flags]
    public enum MultiLineType
    {
        None = 0,
        MultiLineStart = 1, 
        MultiLineLine = 2,
        MultiLineEnd = 4
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