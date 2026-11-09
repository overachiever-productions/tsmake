// NOTE: Some of the directives below are NOT needed from within VS, but ARE needed by PowerShell:
global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
using tsmake.data_models;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    public static RegexOptions BatchSplittingOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline;

    public static readonly Regex BuildDirectives = new(
        @"^\s*--\s*##(?<directive>ROOT|OUTPUT|FILE-MARKER|VERSION-CHECKER):\s*(?<data>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex DirectiveRegex = new(
        @"^\s*--\s*##(?<directive>ROOT|OUTPUT|RUNNER|FILE-MARKER|VERSION-CHECKER|FILE|DIRECTORY|COMMENT|:|(FLAG|FLAGS)-FILE\s*\(#?[a-z]+\)|FLAG-DIRECTORY|CONDITION-DEFAULT|CONDITION|CONDITION-END):\s*(?<data>.*)$",
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
        // Absolute - UNC Share
        if (input.StartsWith(@"\\\\", StringComparison.OrdinalIgnoreCase))
            return PathType.Absolute;

        if (input.StartsWith(@"~\", StringComparison.OrdinalIgnoreCase))
            return PathType.Rooted;

        // Absolute - Local File
        if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.SingleLineRegexOptions))
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

    public static List<string> PrioritizeAndExclude(this List<(string FileName, DateTime TimeStamp)> sourceFiles, DirectoryOptions options)
    {
        if (sourceFiles == null || sourceFiles.Count == 0)
            return new List<string>();

        // 1. Exclusions first - so an excluded file can never be pinned back in by a Sort entry.
        var pool = sourceFiles.ToList();
        if (options?.HasExclude == true)
            pool = pool.Where(f => !options.Exclude!.Any(exclude => f.FileName.Contains(exclude, StringComparison.OrdinalIgnoreCase))).ToList();

        // 2. Work out the sort marker plus the "before" / "after" pinned entries.
        var marker = DirectorySortMarker.Alpha;
        var before = new List<string>();
        var after = new List<string>();

        if (options?.HasSort == true)
        {
            marker = options.SortMarker;
            int markerIndex = options.SortMarkerPosition;

            if (options.Sort != null)
            {
                before = options.Sort.Take(markerIndex).ToList();
                after = options.Sort.Skip(markerIndex + 1).ToList();
            }
        }

        // 3. Pull the pinned files out of the pool (order of the Sort entries is preserved).
        //    "Before" is processed first, so a file listed on both sides ends up at the front.
        var head = TakeMatches(pool, before);
        var tail = TakeMatches(pool, after);

        // 4. Sort whatever remains.
        IEnumerable<(string FileName, DateTime TimeStamp)> middle = marker switch
        {
            DirectorySortMarker.AlphaDesc =>
                pool.OrderByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase),

            DirectorySortMarker.Timestamp =>
                pool.OrderBy(f => f.TimeStamp)
                    .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),

            DirectorySortMarker.TimestampDesc =>
                pool.OrderByDescending(f => f.TimeStamp)
                    .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),

            _ => // Alpha (default)
                pool.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),
        };

        // 5. Stitch it together: pinned-first + sorted remainder + pinned-last.
        var output = new List<string>(head.Count + pool.Count + tail.Count);
        output.AddRange(head);
        output.AddRange(middle.Select(f => f.FileName));
        output.AddRange(tail);
        return output;
    }

    private static List<string> TakeMatches(List<(string FileName, DateTime TimeStamp)> pool, IEnumerable<string> names)
    {
        var result = new List<string>();
        foreach (var name in names)
        {
            int index = pool.FindIndex(f => IsMatch(f.FileName, name));
            if (index < 0)
                continue;

            result.Add(pool[index].FileName);
            pool.RemoveAt(index);
        }
        return result;
    }

    private static bool IsMatch(string filePath, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        candidate = candidate.Trim();
        return string.Equals(filePath, candidate, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileName(filePath), candidate, StringComparison.OrdinalIgnoreCase);
    }
}