using System;

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
}