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
        // Absolute - UNC Share: \\server\share\...
        //      (NOTE: @"\\" is TWO backslashes. Back when \\ was the ROOTED marker, UNC paths needed \\\\ or UNC\\ instead.)
        if (input.StartsWith(@"\\", StringComparison.Ordinal))
            return PathType.Absolute;

        // Rooted - relative to the ##ROOT directory: ~\...
        if (input.StartsWith(@"~\", StringComparison.Ordinal))
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
        //    Same (case-insensitive, exact or */? wildcard) matching as Sort entries - see CreateMatcher().
        var pool = sourceFiles.ToList();
        if (options?.HasExclude == true)
        {
            var excluded = options.Exclude!.Select(CreateMatcher).ToList();
            pool.RemoveAll(f => excluded.Any(isMatch => isMatch(f.FileName)));
        }

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
                // NOTE: options.Sort holds ONLY the file names - the marker itself is NOT in the list.
                //   SortMarkerPosition = # of names BEFORE the marker, so names [0..position) are pinned first
                //   and names [position..end) are pinned last (i.e., Skip(position) - NOT Skip(position + 1)).
                // No marker (-1): every name is pinned first - as if the (default ALPHA) marker were at the end.
                if (markerIndex < 0)
                    markerIndex = options.Sort.Count;

                before = options.Sort.Take(markerIndex).ToList();
                after = options.Sort.Skip(markerIndex).ToList();
            }
        }

        // 3. Pull the pinned files out of the pool (order of the Sort entries is preserved).
        //    Each file goes to the FIRST entry that matches it - and "before" is processed first,
        //    so a file matched on both sides ends up at the front.
        var head = TakeMatches(pool, before, marker);
        var tail = TakeMatches(pool, after, marker);

        // 4. Sort whatever remains.
        var middle = Order(pool, marker);

        // 5. Stitch it together: pinned-first + sorted remainder + pinned-last.
        var output = new List<string>(head.Count + pool.Count + tail.Count);
        output.AddRange(head);
        output.AddRange(middle.Select(f => f.FileName));
        output.AddRange(tail);
        return output;
    }

    private static List<string> TakeMatches(List<(string FileName, DateTime TimeStamp)> pool, IEnumerable<string> entries, DirectorySortMarker marker)
    {
        var result = new List<string>();
        foreach (var entry in entries)
        {
            var isMatch = CreateMatcher(entry);
            var matches = pool.Where(f => isMatch(f.FileName)).ToList();
            if (matches.Count == 0)
                continue;

            // An exact name matches (at most) one file; a wildcard can match several - which are ordered by the sort marker.
            result.AddRange(Order(matches, marker).Select(f => f.FileName));
            pool.RemoveAll(f => isMatch(f.FileName));
        }
        return result;
    }

    private static IEnumerable<(string FileName, DateTime TimeStamp)> Order(IEnumerable<(string FileName, DateTime TimeStamp)> files, DirectorySortMarker marker) => marker switch
    {
        DirectorySortMarker.AlphaDesc =>
            files.OrderByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase),

        DirectorySortMarker.Timestamp =>
            files.OrderBy(f => f.TimeStamp)
                .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),

        DirectorySortMarker.TimestampDesc =>
            files.OrderByDescending(f => f.TimeStamp)
                .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),

        _ => // Alpha (default)
            files.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),
    };

    // Builds a (case-insensitive) matcher for one [exclude:] or [sort:] entry:
    //   - no wildcards:  exact match against the file name (or the full path).
    //   - wildcards:     '*' = zero or more characters, '?' = exactly one character. Matched against the
    //                    file name - or against the full path if the entry includes a '\' or '/'.
    //                    Every other character (including '.', '(', '+', etc.) is literal.
    private static Func<string, bool> CreateMatcher(string entry)
    {
        string candidate = entry?.Trim() ?? string.Empty;
        if (candidate.Length == 0)
            return _ => false;

        if (candidate.IndexOfAny(['*', '?']) < 0)
            return filePath => string.Equals(filePath, candidate, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(filePath), candidate, StringComparison.OrdinalIgnoreCase);

        // Regex.Escape() turns '*' into '\*' and '?' into '\?' - which are then swapped for their regex equivalents.
        string pattern = "^" + Regex.Escape(candidate).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        bool matchFullPath = candidate.IndexOfAny(['\\', '/']) >= 0;

        return filePath => regex.IsMatch(matchFullPath ? filePath : Path.GetFileName(filePath));
    }
}