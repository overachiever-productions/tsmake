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

    // DIRECTIVES: "-- ##NAME: data" - in one of 3 forms (alternatives):
    //  1. FLAG-FILE / FLAG-DIRECTORY (or FLAGS-...): ANY line that starts with one of these names IS a FLAG directive - well-formed or not -
    //      so that a malformed one is REPORTED (by FlagFileParser/FlagDirectoryParser) instead of silently ignored as an ordinary comment:
    //      no keys, bad key names, a missing ':', 2+ spaces before the ':', etc. (Both parse the ORIGINAL line - not just 'data'.)
    //      When well-formed, the directive name includes the FIRST "(KEY[, KEY...])" - e.g., "FLAG-DIRECTORY(#Azure)" - and 'data' is
    //      everything after its ':'. Otherwise, 'data' is everything after the name (and whatever part of a (KEY) list did match).
    //  2. FLAG / FLAGS / FLAG-DEFAULT / FLAG-END (or FLAGS-...) followed by '(' or ':' - which aren't valid directives: DirectiveParser
    //      throws for these (with a hint for the right directive to use) - and the DirectiveProcessor reports that as a syntax error.
    //  3. Every other directive: "NAME:" - exactly.
    //  NOTE: names must be followed by something other than [a-z0-9_-] - so FLAG-FILES, FLAGGED, etc. are NOT FLAG directives (just comments).
    public static readonly Regex DirectiveRegex = new(
        @"^\s*--\s*##(?:(?<directive>(?:FLAG|FLAGS)-(?:FILE|DIRECTORY)(?![a-z0-9_-])(?:\s*\(\s*\#?[a-z0-9_-]+(?:\s*,\s*\#?[a-z0-9_-]+)*\s*\)\s*)?)(?::\s*)?(?<data>.*)|(?<directive>(?:FLAG|FLAGS)(?:-DEFAULT|-END)?(?![a-z0-9_-]))\s*(?=[(:])(?<data>.*)|(?<directive>ROOT|OUTPUT|RUNNER|FILE-MARKER|VERSION-CHECKER|FILE|DIRECTORY|COMMENT|:|CONDITION-DEFAULT|CONDITION|CONDITION-END):\s*(?<data>.*))$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // TOKENS in .sql files: {{##NAME}} - where everything but NAME is literal, and NAME is UPPER-CASE letters and numbers with
    //      single dashes BETWEEN them (e.g., {{##PROJECT-SITE}}). No whitespace anywhere. CASE-SENSITIVE (i.e., no IgnoreCase):
    //      {{##copyright}}, {{COPYRIGHT}}, {{ ##COPYRIGHT}}, {{##A--B}} and {{##A_B}} are all just text.
    //  (Token DEFINITIONS use the same name rules - but are case-insensitive; see TokenDefinition.)
    public static readonly Regex TokenRegex = new(
        @"\{\{\#\#(?<name>[A-Z0-9]+(?:-[A-Z0-9]+)*)\}\}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // A -- comment (to the end of its line).
    private const string HeaderLineComment = @"
        (?>--[^\r\n]*)";

    // A /* block comment */ - which can be NESTED: /* outer /* inner */ still outer */  (balancing group: /* pushes, */ pops).
    private const string HeaderBlockComment = @"
        (?>
            /\*
            (?>(?: /\*(?<depth>) | \*/(?<-depth>) | (?!/\*|\*/)[\s\S] )*)
            (?(depth)(?!))
            \*/
        )";

    // HEADER COMMENTS: the whitespace and comments at the TOP of a file - i.e., everything before its FIRST code.
    //  - comments: -- comments and /* block comments */ (nested or not) - in any combination.
    //  - code: anything else.
    //  - there's ONLY a header if a comment comes before any code - i.e., leading whitespace by itself is NOT a header.
    //  - WARNING (as designed - for now): tsmake DIRECTIVES are -- comments too. So any directive (-- ##FILE:, -- ##DIRECTORY:, etc.)
    //      that comes BEFORE a file's first code is part of its header - and is REMOVED (i.e., never processed) when header comments
    //      are removed. To keep them: don't remove header comments, or move the directives below the first block of code.
    //  - the match (i.e., what gets removed) ends:
    //      a. at \z - when the file is nothing BUT whitespace and comments.
    //      b. at the START of the line where code begins - i.e., whole lines are removed, and the code's indentation is kept.
    //      c. at the code itself - when a block comment ends ON the code's line:   /* ... */USE [master];
    //  NOTE: H* is greedy - so (b)/(c) are found by backing off from the furthest point the header could reach.
    public static readonly Regex HeaderCommentsRegex = new(@"
        \A
        (?=\s*(?:" + HeaderLineComment + "|" + HeaderBlockComment + @"))      # a comment comes before any code
        (?:\s|" + HeaderLineComment + "|" + HeaderBlockComment + @")*         # whitespace and comments
        (?:
            \z                                                                # a. no code at all
          | (?<=[\r\n])(?=[^\S\r\n]*\S)                                       # b. start of the code's line
          | (?<=\*/[^\S\r\n]*)(?=\S)                                          # c. a block comment ends on the code's line
        )",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // COMMENTS (for CommentRemover): a T-SQL 'lexer' - i.e., each match is ONE of the regions that can hide comment markers:
    //      strings, [identifiers], "identifiers", /* block comments */ and -- comments. Matches are found left-to-right (the same
    //      way SQL Server reads T-SQL) and each one is consumed WHOLE - so a delimiter only counts when it's in CODE:
    //      '-- not a comment', [/* not a comment */], /* -- not an EOL comment */, -- not a /* block comment, -- don't (no string) etc.
    //  - strings, identifiers: '' ]] and "" are escapes. Unterminated: run to the end of the text (like SQL Server).
    //  - block comments: can be NESTED (balancing group: /* pushes, */ pops).
    //      Unterminated: 'unterminated' - runs to the end of the text (which SQL Server treats as comment) - so NOTHING after it is
    //      ever treated as code. (Unterminated strings, identifiers and comments are already reported by the Normalizer.)
    //  - -- comments: to the end of the line (the line break is NOT part of the match).
    //  PERF: every match starts with ' [ " / or - (so the regex engine can jump straight to candidates); the block-comment loop eats
    //      runs of [^/*] at a time.
    public static readonly Regex CommentsRegex = new("""
          (?<string>        '[^']*(?:''[^']*)*(?:'|\z) )
        | (?<identifier>    \[[^\]]*(?:\]\][^\]]*)*(?:\]|\z)
                          | "[^"]*(?:""[^"]*)*(?:"|\z) )
        | (?<block>         /\*
                            (?>(?: [^/*]+ | /\*(?<depth>) | \*/(?<-depth>) | /(?!\*) | \*(?!/) )*)
                            (?(depth)(?!))
                            \*/ )
        | (?<unterminated>  /\*[\s\S]* )
        | (?<eol>           --[^\r\n]* )
        """,
        RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.Compiled);
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

    public static string ToLineEnding(this LineEndingsType lineEndingsType) => lineEndingsType switch
    {
        LineEndingsType.CrLf => "\r\n",
        LineEndingsType.Lf => "\n",
        LineEndingsType.Cr => "\r",
        _ => throw new InvalidEnumArgumentException(nameof(lineEndingsType), (int)lineEndingsType, typeof(LineEndingsType))
    };

    // Serializes code-lines into ONE string: each line's TransformedContent (if any - an empty string counts) or else its OriginalContent,
    //      with lineEndingsType BETWEEN lines (none after the last one: a file that ends with a line-break already ends with an empty line).
    //  PERF: one pass to collect each line's content, then string.Join() - which sizes the result up front and copies each line exactly once.
    public static string Serialize(this IReadOnlyList<ICodeLine> codeLines, LineEndingsType lineEndingsType)
    {
        ArgumentNullException.ThrowIfNull(codeLines);

        int count = codeLines.Count;
        if (count == 0)
            return string.Empty;

        var contents = new string[count];
        for (int i = 0; i < count; i++)
            contents[i] = codeLines[i].GetContent();

        return string.Join(lineEndingsType.ToLineEnding(), contents);
    }

    internal static PathType GetPathType(this string input)
    {
        // Absolute - UNC Share: \\server\share\...
        if (input.StartsWith(@"\\", StringComparison.Ordinal))
            return PathType.Absolute;

        // Rooted - relative to the ##ROOT directory: ~\...
        if (input.StartsWith(@"~\", StringComparison.Ordinal))
            return PathType.Rooted;

        // Absolute:
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