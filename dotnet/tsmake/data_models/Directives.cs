namespace tsmake.data_models;

public interface IDirective
{
    public string DirectiveName { get; }
    public string Payload { get; }
    public ICodeLine CodeLine { get; }
    public bool IsValid { get; }
    public string ValidationMessage { get; }
}

public abstract class BaseDirective(string payload, ICodeLine codeLine) : IDirective
{
    public string DirectiveName { get; protected set; } = "BaseDirective";
    public string Payload { get; protected set; } = payload.StripDirectiveComments() ?? string.Empty;
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
        base.DirectiveName = "RootDirective";
        this.FileSystem = fileSystem;

        this.PathType = this.Path.GetPathType();

        // TODO: create a helper method to resolve a path from ... the PAYLOAD + PATHTYPE + CurrentWorkingDirectory (from the IFileSystem)... 
        //      actually ... the IFileSystem should have a method to do the above... 
        string fullPath = this.PathType switch
        {
            PathType.NotSet => string.Empty,
            //PathType.Relative => Path.Combine(this.FileSystem.GetCurrentDirectory(), this.Payload),
            PathType.Absolute => this.Payload,
            PathType.Rooted => this.Payload,
            _ => throw new ArgumentOutOfRangeException(nameof(this.PathType), this.PathType, "Unknown PathType.")
        };

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

    public OutputDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "OutputDirective";
        this.FileSystem = fileSystem;

        // ditto-ish on the path. 
        //    ##OUTPUT should be for a FILE OR for a DIRECTORY.
        //    just make sure that the DIRECTORY (of the FILE if it's a file) exists... 
    }
}

public class FileMarkerDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public FileMarkerDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "FileMarkerDirective";
        this.FileSystem = fileSystem;
    }
}

public class CommentDirective : BaseDirective
{
    public CommentDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "CommentDirective";

        // DONE here ... 

        // BUT ... when the assembler goes to .Serialize() all .CodeLines ... 
        //   it should skip over any CommentDirective instances ... 
    }
}

public class FileDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public string FilePath { get; private set; }

    public FileDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "FileDirective";
        this.FileSystem = fileSystem;

        this.FilePath = this.Payload;
    }
}

public class DirectoryDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public DirectoryDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "DirectoryDirective";
        this.FileSystem = fileSystem;
    }
}

public class ConditionalFileDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public ConditionalFileDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "ConditionalFileDirective";
        this.FileSystem = fileSystem;
    }
}

public class ConditionalDirectoryDirective : BaseDirective
{
    private IFileSystem FileSystem { get; set; }

    public ConditionalDirectoryDirective(string payload, ICodeLine codeLine, IFileSystem fileSystem) : base(payload, codeLine)
    {
        base.DirectiveName = "ConditionalDirectoryDirective";
        this.FileSystem = fileSystem;
    }
}

public class ConditionDefaultDirective : BaseDirective
{
    public ConditionDefaultDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "ConditionDefaultDirective";
    }
}

public class ConditionDirective : BaseDirective
{
    public ConditionDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "ConditionDirective";
    }
}

public class ConditionEndDirective : BaseDirective
{
    public ConditionEndDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "ConditionEndDirective";
    }
}

public class RunnerDirective : BaseDirective
{
    public RunnerDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "RunnerDirective";
    }
}

public class VersionCheckerDirective : BaseDirective
{
    public VersionCheckerDirective(string payload, ICodeLine codeLine) : base(payload, codeLine)
    {
        base.DirectiveName = "VersionCheckerDirective";
    }
}

public class DirectiveParser
{
    public static IDirective LoadDirective(string directiveName, string payload, ICodeLine codeLine, IFileSystem fileSystem)
    {
        // ROOT|OUTPUT|RUNNER|FILEMARKER|VERSION-CHECKER|FILE|DIRECTORY|COMMENT|:|CONDITIONAL-FILE|CONDITIONAL-DIRECTORY|CONDITION-DEFAULT|CONDITION|CONDITION-END
        return directiveName.ToUpperInvariant() switch
        {
            "ROOT" => new RootDirective(payload, codeLine, fileSystem),
            "OUTPUT" => new OutputDirective(payload, codeLine, fileSystem),
            "RUNNER" => new RunnerDirective(payload, codeLine),
            "FILEMARKER" => new FileMarkerDirective(payload, codeLine, fileSystem),
            "VERSION-CHECKER" => new VersionCheckerDirective(payload, codeLine),
            ":" => new CommentDirective(payload, codeLine),
            "COMMENT" => new CommentDirective(payload, codeLine),
            "FILE" => new FileDirective(payload, codeLine, fileSystem),
            "DIRECTORY" => new DirectoryDirective(payload, codeLine, fileSystem),

            // NOTE: all conditionals (other than Condition-End) are going to need some PART of the IAssemblyOptions... i.e., .BuildFlags or whatever... 
            "CONDITIONAL-FILE" => new ConditionalFileDirective(payload, codeLine, fileSystem),
            "CONDITIONAL-DIRECTORY" => new ConditionalDirectoryDirective(payload, codeLine, fileSystem),
            "CONDITION-DEFAULT" => new ConditionDefaultDirective(payload, codeLine),
            "CONDITION" => new ConditionDirective(payload, codeLine),
            "CONDITION-END" => new ConditionEndDirective(payload, codeLine),
            _ => throw new ArgumentException($"Unknown directive name: {directiveName}", nameof(directiveName)),
        };
    }
}