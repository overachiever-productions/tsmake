namespace tsmake.data_models;

public interface IInclude
{
    List<string> IncludedFiles { get; }
}

public abstract class BaseInclude(IDirective directive, IFileSystem fileSystem, List<IError> syntaxErrors) : IInclude
{
    internal IDirective Directive { get; } = directive;
    internal IFileSystem FileSystem { get; } = fileSystem;
    internal List<IError> SyntaxErrors { get; } = syntaxErrors;

    public List<string> IncludedFiles { get; protected set; } = new List<string>();
}

public class FileInclude(IDirective directive, IFileSystem fileSystem, List<IError> syntaxErrors) : BaseInclude(directive, fileSystem, syntaxErrors);   

public class DirectoryInclude(IDirective directive, IFileSystem fileSystem, List<IError> syntaxErrors) : BaseInclude(directive, fileSystem, syntaxErrors);   

public class VersionCheckerInclude(IDirective directive, IFileSystem fileSystem, List<IError> syntaxErrors) : BaseInclude(directive, fileSystem, syntaxErrors);   

public class IncludeTranslator
{
    public static IInclude LoadInclude(IDirective directive, IFileSystem fileSystem, List<ICodeLine> codeLines, List<IError> syntaxErrors)
    {
        if (directive.DirectiveName == "File")
            return new FileInclude(directive, fileSystem, syntaxErrors);

        if (directive.DirectiveName == "Directory")
            return new DirectoryInclude(directive, fileSystem, syntaxErrors);

        if (directive.DirectiveName == "Version-Checker")
            return new VersionCheckerInclude(directive, fileSystem, syntaxErrors);

        throw new Exception($"tsmake Workflow Exception: Unrecognized directive type: {directive.DirectiveName}");
    }
}