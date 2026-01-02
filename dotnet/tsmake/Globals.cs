// NOTE: Some of the directives below are NOT needed from within VS, but ARE needed by PowerShell:
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Management.Automation;
global using System.Text.RegularExpressions;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    public static RegexOptions BatchSplittingOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline;
}

public class CodeLine(int lineNumber, int startOffset, int endOffset, string text)
{
    public int LineNumber { get; } = lineNumber;
    public int StartOffset { get; } = startOffset;
    public int EndOffset { get; } = endOffset;            // position of the END of the line - sans cr/lf/crlf
    public string Text { get; } = text;

    public int Length => this.EndOffset - this.StartOffset + 1;
}