// NOTE: Some of the directives below are NOT needed from within VS, but ARE 100% needed by PowerShell:
global using System;
global using System.IO;
global using System.Text;
global using System.Linq;
global using System.Collections.Generic;
global using System.Text.RegularExpressions;
global using System.Management.Automation;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
}

// REFACTOR: move this OUT of globals as an Extension Method and ... just shove it into a logical spot as a PRIVATE method within the tokenizer? (or wherever it's used)
public static class ExtensionMethods
{
    public static string ReplaceAtIndex(this string source, int index, char replacement)
    {
        if (source == null) throw new ArgumentNullException("source");

        StringBuilder builder = new StringBuilder(source);
        builder[index] = replacement;
        return builder.ToString();
    }
}

public class SyntaxException(string message, int line, int lineStart, int start, int end, ISourceLine sourceLine = null) : Exception(message)
{
    public int LineNumber { get; } = line;
    public ISourceLine SourceLine { get; } = sourceLine;
    public int LineOffsetStart { get; } = lineStart;
    public int OffsetStart { get; } = start;
    public int OffsetEnd { get; } = end;
}