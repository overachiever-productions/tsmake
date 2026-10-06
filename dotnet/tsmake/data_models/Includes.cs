namespace tsmake.data_models;

public interface IInclude
{
    List<string> IncludedFiles { get; }
}

public abstract class BaseInclude(IDirective directive, IFileSystem fileSystem, List<ISyntaxError> syntaxErrors) : IInclude
{
    internal IDirective Directive { get; } = directive;
    internal IFileSystem FileSystem { get; } = fileSystem;
    internal List<ISyntaxError> SyntaxErrors { get; } = syntaxErrors;

    public List<string> IncludedFiles { get; protected set; } = new List<string>();
}

public class FileInclude : BaseInclude
{
    public FileInclude(IDirective directive, IFileSystem fileSystem, List<ISyntaxError> syntaxErrors) : base(directive, fileSystem, syntaxErrors)
    {
        base.IncludedFiles.Add(((FileDirective)directive).FilePath);
    }
}   

public class DirectoryInclude : BaseInclude
{
    public DirectoryInclude(IDirective directive, IFileSystem fileSystem, List<ISyntaxError> syntaxErrors) : base(directive, fileSystem, syntaxErrors)
    {

    }
}

public class VersionCheckerInclude : BaseInclude
{
    public VersionCheckerInclude(IDirective directive, IFileSystem fileSystem, List<ISyntaxError> syntaxErrors) : base(directive, fileSystem, syntaxErrors)
    {

    }
}

public class IncludeTranslator
{
    public static IInclude LoadInclude(IDirective directive, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
    {
        if (directive.DirectiveType == DirectiveType.File)
            return new FileInclude(directive, fileSystem, syntaxErrors);

        if (directive.DirectiveType == DirectiveType.Directory)
            return new DirectoryInclude(directive, fileSystem, syntaxErrors);

        if (directive.DirectiveType == DirectiveType.VersionChecker)
            return new VersionCheckerInclude(directive, fileSystem, syntaxErrors);

        throw new Exception($"tsmake Workflow Exception: Unrecognized directive type: {directive.DirectiveType}");
    }
}