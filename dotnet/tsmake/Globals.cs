// NOTE: Some of the directives below are NOT needed from within VS, but ARE needed by PowerShell:
global using System;
global using System.IO;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.ComponentModel;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    public static RegexOptions BatchSplittingOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline;

    public static readonly Regex DirectiveRegex = new(
        @"^\s*--\s*##(?<directive>ROOT|OUTPUT|RUNNER|FILEMARKER|VERSION-CHECKER|FILE|DIRECTORY|COMMENT|:|CONDITIONAL-FILE|CONDITIONAL-DIRECTORY|CONDITION-DEFAULT|CONDITION|CONDITION-END):\s*(?<data>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
}

public static class tsmakeExtensions
{
    public static int Priority(this SourceType sourceType) => sourceType switch
    {
        SourceType.CommandLine => 1,
        SourceType.ConfigFile => 2,
        SourceType.BuildFile => 3,
        SourceType.Convention => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "No priority defined for this SourceType.")
    };

    internal static PathType GetPathType(this string input)
    {
        if (input.StartsWith(@"\\\"))
            return PathType.Rooted;

        // Absolute - Local File
        if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.SingleLineRegexOptions))
            return PathType.Absolute;

        // Absolute - UNC Share
        if (input.StartsWith("//"))
            return PathType.Absolute;

        return PathType.Relative;
    }

    public static string StripDirectiveComments(this string input)
    {
        int index = input.IndexOf("##::", StringComparison.Ordinal);
        if (index >= 0)
            return input.Substring(0, index).Trim();
        
        return input;
    }
}