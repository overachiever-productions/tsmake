using System;

namespace tsmake.enums
{
    [Flags]
    public enum LineType
    {
        RawContent = 1,
        WhitespaceOnly = 2, 
        TokenizedContent = 4,
        Directive = 8
    }
}