namespace tsmake.data_models;

public record FlagIncludeOption(string Key, string Path);

// Parsed components of a ##DIRECTORY payload: path [exclude: ...] [sort: ...]
//   Exclude / Sort are NULL when their clause is absent (Sort is EMPTY - not null - for marker-only clauses like [sort: ALPHA]).
//   SortMarkerPosition = index within Sort where the marker appeared (i.e., # of explicit names before it), or -1 if no marker.
public record DirectoryOptions(
    string Path,
    IReadOnlyList<string>? Exclude,
    IReadOnlyList<string>? Sort,
    DirectorySortMarker SortMarker,
    int SortMarkerPosition)
{
    public bool HasExclude => this.Exclude is not null;
    public bool HasSort => this.Sort is not null;
}

public interface IFlagIncludeResolver
{
    public FlagIncludeOption? ResolveOptionFromFlags(List<string> flags);
}

public interface IDirective
{           
    public DirectiveType DirectiveType { get; }
    public string Payload { get; }              
    public ICodeLine CodeLine { get; }
    public bool IsValid { get; }
    public string ValidationMessage { get; }
}

public abstract class BaseDirective(string payload, ICodeLine codeLine) : IDirective
{
    public DirectiveType DirectiveType { get; protected set; } = DirectiveType.Comment; // default/placeholder - overridden by derived classes
    public string Payload { get; protected set; } = payload.StripDirectiveComments().Trim() ?? string.Empty;
    public ICodeLine CodeLine { get; protected set; } = codeLine;
    public bool IsValid { get; protected set; } = false;
    public string ValidationMessage { get; protected set; } = string.Empty;
}

public class RootDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public string Path { get; private set; } = string.Empty;
    public PathType PathType { get; private set; } = PathType.NotSet;

    public RootDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Root;
        this.FileSystem = fileSystem;

        string fullPath = this.FileSystem.TranslatePath(this.Payload);

        if (this.FileSystem.DirectoryExists(fullPath))
        {
            this.Path = fullPath;
            this.PathType = this.Path.GetPathType();

            base.IsValid = true;
        }
        else
        {
            base.IsValid = false;
            base.ValidationMessage = $"The specified ##ROOT path does not exist: {this.Payload}";
        }
    }
}

public class OutputDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public string Path { get; private set; } = string.Empty;
    public PathType PathType { get; private set; } = PathType.NotSet;

    public OutputDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Output;
        this.FileSystem = fileSystem;

        string fullPath = this.FileSystem.TranslatePath(this.Payload);

        // NOTE: Checking ONLY for directory.exists at this point. ARTIFACT will have to deal with FILE-NAME within the directory-in-question at write-time/etc. 
        if(this.FileSystem.DirectoryExists(fullPath))
        {
            base.IsValid = true;
            this.Path = fullPath;
            this.PathType = this.Path.GetPathType();
        }
        else
        {
            base.IsValid = false;
            base.ValidationMessage = $"The specified ##OUTPUT path does not exist: {fullPath}";
        }
    }
}

public class FileMarkerDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public FileMarkerDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.FileMarker;
        this.FileSystem = fileSystem;
    }
}

public class CommentDirective : BaseDirective
{
    public CommentDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Comment;
        base.IsValid = true;
    }
}

public class FileDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public string FilePath { get; private set; }

    public FileDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.File;
        this.FileSystem = fileSystem;

        try
        {
            this.FilePath = this.FileSystem.TranslatePath(this.Payload);
            if(this.FileSystem.FileExists(this.FilePath))
                base.IsValid = true;
            else
            {
                base.IsValid = false;
                base.ValidationMessage = $"Specified ##FILE does not exist: {this.FilePath}";
            }
        }
        catch (Exception ex)
        {
            this.FilePath = string.Empty;
            base.IsValid = false;
            base.ValidationMessage = ex.Message;
        }
    }
}

public class DirectoryDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public string FilePath { get; private set; }

    public DirectoryDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Directory;
        this.FileSystem = fileSystem;

        try
        {
            this.FilePath = this.FileSystem.TranslatePath(this.Payload);
            if (this.FileSystem.FileExists(this.FilePath))
                base.IsValid = true;
            else
            {
                base.IsValid = false;
                base.ValidationMessage = $"Specified ##DIRECTORY does not exist: {this.FilePath}";
            }
        }
        catch (Exception ex)
        {
            this.FilePath = string.Empty;
            base.IsValid = false;
            base.ValidationMessage = ex.Message;
        }
    }
}

public class FlagFileDirective : BaseDirective, IFlagIncludeResolver
{
    private IFileSystem FileSystem { get; set; }
    private Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

    public FlagFileDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.FlagFile;
        this.FileSystem = fileSystem;

        try
        {
            string validation = "";
            var options = FlagFileParser.Parse(this.CodeLine.OriginalContent);
            foreach(var (key, value) in options)
            {
                string fullPath = this.FileSystem.TranslatePath(value);
                if (!this.FileSystem.FileExists(fullPath))
                    validation += $"Path specified for ({key}): does not exist: {value}\r\n";
            }

            this.Options = options;

            if (!string.IsNullOrWhiteSpace(validation))
            {
                base.IsValid = false;
                base.ValidationMessage = validation.Trim();
            }
            else
                base.IsValid = true;
        }
        catch(Exception ex)
        {
            base.IsValid = false;
            base.ValidationMessage = ex.Message;
        }
    }

    public FlagIncludeOption? ResolveOptionFromFlags(List<string> flags)
    {
        // TODO ... implement this HERE ... as an initial proof of concept. 
        //   THEN ... create an extension method for ... IFlagIncludeResolver.ResolveOptionFromFlags() 
        //      (if it's possible to hang an extension method off/from an interface) 
        //        that takes in a Dictionary<string, string> options and a List<string> flags
        //          along with a DEFAULT or NULL/EMPTY if no match is found ... (in DEFAULT -> empty order).

        if (this.Options.Count == 0)
            return null;    

        foreach(var flag in flags)
        {
            if(this.Options.TryGetValue(flag, out var option))
                return new FlagIncludeOption(flag, option);
        }

        if (this.Options.TryGetValue("DEFAULT", out var defaultOption))
            return new FlagIncludeOption("DEFAULT", defaultOption);

        return null;
    }   
}

public class FlagDirectoryDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public FlagDirectoryDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.FlagDirectory;
        this.FileSystem = fileSystem;
    }
}

public class ConditionDefaultDirective : BaseDirective
{
    private List<ConditionDirective> _conditions { get; set; } = new List<ConditionDirective>();
    private ConditionEndDirective? _conditionEnd { get; set; } = null;

    public ConditionDefaultDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.ConditionDefault;
    }

    public void AddCondition(ConditionDirective condition)
    {
        this._conditions.Add(condition);
    }

    public void AddConditionEnd(ConditionEndDirective conditionEnd)
    {
        this._conditionEnd = conditionEnd;
    }
}

public class ConditionDirective : BaseDirective
{
    public ConditionDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Condition;
    }
}

public class ConditionEndDirective : BaseDirective
{
    public ConditionEndDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.ConditionEnd;
    }
}

public class RunnerDirective : BaseDirective
{
    public RunnerDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Runner;
    }
}

public class VersionCheckerDirective : BaseDirective
{
    public VersionCheckerDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.VersionChecker;
    }
}

public class DirectiveParser
{
    // REFACTOR: ?? Use a C# Union: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union
    public static IDirective LoadDirective(string directiveName, string payload, ICodeLine codeLine, IFileSystem fileSystem)
    {
        if(directiveName.Contains("("))
            directiveName= directiveName.Substring(0, directiveName.IndexOf("(")).Trim();

        // ROOT|OUTPUT|RUNNER|FILE-MARKER|VERSION-CHECKER|FILE|DIRECTORY|COMMENT|:|FLAG-FILE|FLAG-DIRECTORY|CONDITION-DEFAULT|CONDITION|CONDITION-END
        return directiveName.ToUpperInvariant() switch
        {
            "ROOT" => new RootDirective(payload, codeLine, fileSystem),
            "OUTPUT" => new OutputDirective(payload, codeLine, fileSystem),
            "RUNNER" => new RunnerDirective(payload, codeLine),
            "FILE-MARKER" => new FileMarkerDirective(payload, codeLine, fileSystem),
            "VERSION-CHECKER" => new VersionCheckerDirective(payload, codeLine),
            ":" => new CommentDirective(payload, codeLine),
            "COMMENT" => new CommentDirective(payload, codeLine),
            "FILE" => new FileDirective(payload, codeLine, fileSystem),
            "DIRECTORY" => new DirectoryDirective(payload, codeLine, fileSystem),
            "CONDITION-DEFAULT" => new ConditionDefaultDirective(payload, codeLine),
            "CONDITION" => new ConditionDirective(payload, codeLine),
            "CONDITION-END" => new ConditionEndDirective(payload, codeLine),

            // NOTE: all FLAG-X (other than FLAG-END) are going to need some PART of the IAssemblyOptions... i.e., .BuildFlags or whatever... 
            //    or ... maybe it just needs an option to have .FLAGS passed in when being processed? 
            "FLAG-FILE" or "FLAGS-FILE" => new FlagFileDirective(payload, codeLine, fileSystem),
            "FLAG-DIRECTORY" or "FLAGS-DIRECTORY" => new FlagDirectoryDirective(payload, codeLine, fileSystem),

            "FLAG" or "FLAGS" => throw new ArgumentException($"Directive '{directiveName}' is not valid. Use FLAG-FILE or FLAG-DIRECTORY instead.", nameof(directiveName)),
            "FLAG-DEFAULT" or "FLAGS-DEFAULT" => throw new ArgumentException($"Directive '{directiveName}' is not valid. Use CONDITION-DEFAULT instead.", nameof(directiveName)),
            "FLAG-END" or "FLAGS-END" => throw new ArgumentException($"Directive '{directiveName}' is not valid. Use CONDITION-END instead.", nameof(directiveName)),

            _ => throw new ArgumentException($"Unknown directive name: {directiveName}", nameof(directiveName)),
        };
    }
}

public static class FlagFileParser
{
    // One "(KEY[, KEY...]): path" entry. \G forces each match to begin where the last one ended.
    private static readonly Regex Entry = new Regex(@"
        \G\s*
        \(\s*
            \#?(?<key>[A-Za-z0-9_-]+)                  # first name; the '#' is not captured
            (?:\s*,\s*\#?(?<key>[A-Za-z0-9_-]+))*      # more comma-separated names (each captured)
        \s*\)
        \x20?:\s*                                      # REQUIRED colon, 0-1 spaces before it
        (?:
            ""(?<path>[^""]+)""\s*                     # ""double-quoted"" path (parens allowed)
          | '(?<path>[^']+)'\s*                        # 'single-quoted' path (parens allowed)
          | (?<path>[^\s""'()](?:[^""'()]*[^\s""'()])?)(?:\s+|$)
                                                       # unquoted path: no quotes or parens, and must be
                                                       # followed by whitespace or end of string
        )
        (?=\(|$)                                       # next must be another directive or the end
        ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    // Used only for error messages: a syntactically valid "(...)" directive.
    private static readonly Regex DirectiveStart = new Regex(
        @"^\s*\(\s*\#?[A-Za-z0-9_-]+(?:\s*,\s*\#?[A-Za-z0-9_-]+)*\s*\)");

    public static Dictionary<string, string> Parse(string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        int start = input.IndexOf('(');
        if (start < 0)
            throw new FormatException("No (DIRECTIVE) found.");

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int pos = start;

        for (Match m = Entry.Match(input, start); m.Success; m = m.NextMatch())
        {
            string path = m.Groups["path"].Value;
            foreach (Capture key in m.Groups["key"].Captures)
            {
                string keyValue = key.Value.ToUpperInvariant();
                if (!string.Equals(keyValue, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                    keyValue = $"#{keyValue}";

                if (!result.TryAdd(keyValue, path))
                    throw new FormatException($"Duplicate directive '{keyValue}'.");
            }
            pos = m.Index + m.Length;
        }

        if (pos < input.Length)
            throw new FormatException(Describe(input, pos));

        return result;
    }

    private static string Describe(string input, int pos)
    {
        string rest = input.Substring(pos);
        Match d = DirectiveStart.Match(rest);

        string hint =
            !d.Success
                ? "Expected a directive like (DEFAULT) or (#RDS, #Azure); names may only contain A-Z, a-z, 0-9, '-' and '_'."
            : !Regex.IsMatch(rest.Substring(d.Length), @"^\x20?:")
                ? $"Missing ':' after '{d.Value.Trim()}' (at most one space is allowed before it)."
            : "Paths containing '(' or ')' or quote characters must be wrapped in \"double\" or 'single' quotes.";

        return $"Invalid flag-file syntax at position {pos}, near: {rest}\n{hint}";
    }
}

public static class DirectoryParser
{
    private const RegexOptions Options =
        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    // The REQUIRED directory path - at the very start of the payload.
    private static readonly Regex PathPattern = new Regex(@"
        \A\s*
        (?:
            ""(?<path>[^""]+)""                                 # ""double-quoted"" path (commas, parens, brackets allowed)
          | '(?<path>[^']+)'                                    # 'single-quoted' path (commas, parens, brackets allowed)
          | (?<path>(?:(?!\[\s*(?:exclude|sort)\s*:)[^""'(),])+?)
                                                                # unquoted path: spaces are fine, but no quotes, parens
                                                                # or commas; it ends where a clause begins
        )
        \s*
        (?=\[\s*(?:exclude|sort)\s*:|\z)                        # next must be a clause or the end
        ", Options);

    // One [exclude: ...] or [sort: ...] clause. \G forces each match to begin where the last one ended.
    private static readonly Regex ClausePattern = new Regex(@"
        \G\s*
        \[\s*(?<kind>exclude|sort)\s*:
        (?<body>(?:""[^""]*""|'[^']*'|[^\]""'])*)               # quoted entries may contain ']'
        \]
        ", Options);

    // One comma-delimited entry within a clause's body.
    private static readonly Regex EntryPattern = new Regex(@"
        \G\s*
        (?:
            ""(?<quoted>[^""]*)""                               # ""double-quoted"" entry
          | '(?<quoted>[^']*)'                                  # 'single-quoted' entry
          | (?<bare>[^\s""'(),\[\]](?:[^""'(),\[\]]*[^\s""'(),\[\]])?)
                                                                # unquoted entry: spaces are fine, but no quotes,
                                                                # parens, commas or brackets
        )
        \s*
        (?:(?<sep>,)|\z)                                        # a comma, or the end of the clause
        ", Options);

    // Used only for error messages: anything shaped like a "[name:" clause opener.
    private static readonly Regex ClauseHead = new Regex(@"\[\s*(?<kind>[A-Za-z][A-Za-z0-9_-]*)\s*:", Options);

    /// <summary>
    /// Parses the PAYLOAD of a ##DIRECTORY directive (everything after "##DIRECTORY:", with any ##:: comment already removed), e.g.:
    ///     Common\Tables [exclude: scratch.sql, "old, unused.sql"] [sort: 00_setup.sql, ALPHA]
    /// Throws FormatException (with a hint) for invalid syntax.
    /// </summary>
    public static DirectoryOptions Parse(string payload)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        string input = payload.Trim();
        if (input.Length == 0)
            throw new FormatException("A ##DIRECTORY path is required.");

        Match p = PathPattern.Match(input);
        if (!p.Success)
            throw new FormatException(DescribePathError(input));

        string path = p.Groups["path"].Value;
        bool quotedPath = input[0] is '"' or '\'';

        if (string.IsNullOrWhiteSpace(path))
            throw new FormatException("A ##DIRECTORY path is required (the quoted path is empty).");

        if (!quotedPath && FindUnknownClause(path, 0) is { } unknown)
            throw new FormatException($"Invalid ##DIRECTORY syntax near: {input}\n{UnknownClauseHint(unknown)}");

        List<string>? exclude = null;
        List<string>? sort = null;
        var marker = DirectorySortMarker.None;
        string markerText = string.Empty;
        int markerPosition = -1;
        int pos = p.Length;

        for (Match c = ClausePattern.Match(input, pos); c.Success; c = c.NextMatch())
        {
            string kind = c.Groups["kind"].Value.ToLowerInvariant();
            var entries = ParseEntries(c.Groups["body"].Value, kind);

            if (kind == "exclude")
            {
                if (exclude is not null)
                    throw new FormatException("Only one [exclude: ...] clause is allowed.");

                foreach (var (value, _) in entries)
                {
                    if (!value.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                        throw new FormatException($"[exclude:] entries must be .sql files; found: '{value}'.");
                }

                exclude = entries.Select(e => e.Value).ToList();
            }
            else
            {
                if (sort is not null)
                    throw new FormatException("Only one [sort: ...] clause is allowed.");

                sort = new List<string>();
                foreach (var (value, quoted) in entries)
                {
                    // Quoting a marker (e.g., 'ALPHA') makes it a literal name instead.
                    var entryMarker = quoted ? DirectorySortMarker.None : ToMarker(value);
                    if (entryMarker == DirectorySortMarker.None)
                    {
                        sort.Add(value);
                        continue;
                    }

                    if (marker != DirectorySortMarker.None)
                        throw new FormatException($"Only one sort marker (ALPHA, ALPHADESC, TIMESTAMP or TIMESTAMPDESC) is allowed; found '{markerText}' and '{value}'.");

                    marker = entryMarker;
                    markerText = value;
                    markerPosition = sort.Count;
                }
            }

            pos = c.Index + c.Length;
        }

        if (pos < input.Length)
            throw new FormatException(DescribeClauseError(input, pos));

        return new DirectoryOptions(path, exclude, sort, marker, markerPosition);
    }

    private static List<(string Value, bool Quoted)> ParseEntries(string body, string kind)
    {
        var entries = new List<(string Value, bool Quoted)>();
        bool trailingComma = false;
        int pos = 0;

        for (Match m = EntryPattern.Match(body); m.Success; m = m.NextMatch())
        {
            bool quoted = m.Groups["quoted"].Success;
            string value = quoted ? m.Groups["quoted"].Value : m.Groups["bare"].Value;

            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"Empty quoted entry in [{kind}: {body.Trim()}].");

            entries.Add((value, quoted));
            trailingComma = m.Groups["sep"].Success;
            pos = m.Index + m.Length;
        }

        string rest = body.Substring(pos).Trim();
        if (rest.Length > 0)
        {
            string hint = rest.StartsWith(',')
                ? "Empty entry (a leading comma, or two commas in a row)."
                : "Entries containing ',', '(', ')', '[', ']' or quote characters must be wrapped in \"double\" or 'single' quotes.";

            throw new FormatException($"Invalid [{kind}:] entry near: {rest}\n{hint}");
        }

        if (entries.Count == 0)
            throw new FormatException($"[{kind}:] clause is empty; expected a comma-delimited list.");

        if (trailingComma)
            throw new FormatException($"[{kind}:] clause ends with a trailing comma.");

        return entries;
    }

    private static DirectorySortMarker ToMarker(string value) => value.ToUpperInvariant() switch
    {
        "ALPHA" => DirectorySortMarker.Alpha,
        "ALPHADESC" => DirectorySortMarker.AlphaDesc,
        "TIMESTAMP" => DirectorySortMarker.Timestamp,
        "TIMESTAMPDESC" => DirectorySortMarker.TimestampDesc,
        _ => DirectorySortMarker.None
    };

    private static Match? FindUnknownClause(string input, int start)
    {
        for (Match m = ClauseHead.Match(input, start); m.Success; m = m.NextMatch())
        {
            string kind = m.Groups["kind"].Value;
            if (!kind.Equals("exclude", StringComparison.OrdinalIgnoreCase) && !kind.Equals("sort", StringComparison.OrdinalIgnoreCase))
                return m;
        }

        return null;
    }

    private static string UnknownClauseHint(Match unknown) =>
        $"Unknown clause '[{unknown.Groups["kind"].Value}:'. Expected [exclude: ...] or [sort: ...]. " +
        "(If the path itself contains '[', wrap it in \"double\" or 'single' quotes.)";

    private static string DescribePathError(string input)
    {
        char first = input[0];
        bool quoted = first is '"' or '\'';
        int closingQuote = quoted ? input.IndexOf(first, 1) : -1;

        string hint =
            quoted && closingQuote < 0
                ? $"The path is missing its closing {first} quote."
            : quoted && closingQuote == 1
                ? "A ##DIRECTORY path is required (the quoted path is empty)."
            : FindUnknownClause(input, quoted ? closingQuote + 1 : 0) is { } unknown
                ? UnknownClauseHint(unknown)
            : !quoted && ClauseHead.Match(input) is { Success: true, Index: 0 }
                ? "A directory path is required before any [exclude: ...] or [sort: ...] clause."
            : quoted
                ? "Only [exclude: ...] and/or [sort: ...] clauses may follow the quoted path."
            : "Paths containing ',', '(', ')' or quote characters must be wrapped in \"double\" or 'single' quotes.";

        return $"Invalid ##DIRECTORY path near: {input}\n{hint}";
    }

    private static string DescribeClauseError(string input, int pos)
    {
        string rest = input.Substring(pos).TrimStart();
        Match head = ClauseHead.Match(rest);
        bool atClause = head.Success && head.Index == 0;

        string hint =
            !atClause
                ? "Expected an [exclude: ...] or [sort: ...] clause, or the end of the directive."
            : FindUnknownClause(head.Value, 0) is { } unknown
                ? UnknownClauseHint(unknown)
            : $"The [{head.Groups["kind"].Value}:] clause is missing its closing ']' (or has an unterminated quote).";

        return $"Invalid ##DIRECTORY syntax at position {pos}, near: {rest}\n{hint}";
    }
}