namespace tsmake.data_models;

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

    public DirectoryDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveType = DirectiveType.Directory;
        this.FileSystem = fileSystem;
    }
}

public class FlagFileDirective : BaseDirective
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
    private ConditionEndDirective _conditionEnd { get; set; } = null;

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