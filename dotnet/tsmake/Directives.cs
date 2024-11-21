namespace tsmake;

public interface IDirective
{
    string DirectiveName { get; }
    ICodeLine CodeLine { get; }  // TODO: code lines need a 'stack' of Location info ... i.e., file-source 'lineage'. 

    int IndexStart { get; }
    int IndexEnd { get; }

    bool IsValid { get; }
    string ValidationMessage { get; }
    string DirectiveData { get; }
}

public interface IFileSystemDirective
{
    string DirectiveName { get; }
    ISourceLine SourceLine { get; }

    int IndexStart { get; }
    int IndexEnd { get; }

    bool IsValid { get; }
    string ValidationMessage { get; }
    string DirectiveData { get; }

    string Path { get; }
    PathType PathType { get; }

    List<string> GetChildren();
}

public class BaseDirective(ICodeLine codeLine, string directiveData, int start, int end) : IDirective
{
    public string DirectiveName { get; protected set; } = "BASE";
    public ICodeLine CodeLine { get; protected set; } = codeLine;
    public int IndexStart { get; protected set; } = start;
    public int IndexEnd { get; protected set; } = end;
    public bool IsValid { get; protected set; } = false;
    public string ValidationMessage { get; protected set; } = string.Empty;

    public string DirectiveData { get; protected set; } = directiveData;
}

public class BaseFileSystemDirective(IFileSystem fileSystem, ISourceLine sourceLine, string directiveData, int start, int end) : IFileSystemDirective
{
    protected List<string> _children = new List<string>();
    private IFileSystem FileSystem = fileSystem;
    public string DirectiveName { get; protected set; } = "BASE";
    public ISourceLine SourceLine { get; protected set; } = sourceLine;
    public int IndexStart { get; protected set; } = start;
    public int IndexEnd { get; protected set; } = end;
    public bool IsValid { get; protected set; } = false;
    public string ValidationMessage { get; protected set; } = string.Empty;

    public string DirectiveData { get; protected set; } = directiveData;

    public string Path { get; protected set; } = string.Empty;
    public PathType PathType { get; protected set; } = PathType.NotSet;

    public List<string> GetChildren()
    {
        return _children;
    }

    protected void AddChild(string path)
    {
        this._children.Add(path);
    }
}

public class RootDirective : BaseFileSystemDirective
{
    public string AbsolutePath { get; private set; }

    internal RootDirective(IFileSystem fileSystem, ISourceLine sourceLine, string directiveData, int start, int end)
        : base(fileSystem, sourceLine, directiveData, start, end)
    {
        this.DirectiveName = "ROOT";

        if (fileSystem.IsValidFilePath(directiveData))
        {
            this.PathType = fileSystem.GetPathType(directiveData);
            this.Path = directiveData;
            this.AbsolutePath = fileSystem.TranslatePath(directiveData, this.PathType);
        }
        else 
            base.ValidationMessage = $"Invalid Path (or path not found) ... for [ROOT] Directive. Payload: [{directiveData}].";
    }
}

public class OutputDirective : BaseFileSystemDirective
{
    internal OutputDirective(IFileSystem fileSystem, ISourceLine sourceLine, string directiveData, int start, int end)
        : base(fileSystem, sourceLine, directiveData, start, end)
    {
        this.DirectiveName = "OUTPUT";
    }
}

public class FileSystemFileDirective : BaseFileSystemDirective
{
    internal FileSystemFileDirective(IFileSystem fileSystem, ISourceLine sourceLine, string directiveData, int start, int end)
        : base(fileSystem, sourceLine, directiveData, start, end)
    {
        this.DirectiveName = "FILE";

        if (fileSystem.IsValidFilePath(directiveData))
        {
            this.PathType = fileSystem.GetPathType(directiveData);
            this.Path = directiveData;

            var absolutePath = fileSystem.TranslatePath(this.Path, this.PathType);
            base.AddChild(absolutePath);

            this.IsValid = true;
        }
        else
            base.ValidationMessage = $"Invalid or Missing File-Path data for [FILE] Directive. Payload: [{directiveData}].";
    }
}

public class FileSystemDirectoryDirective : BaseFileSystemDirective
{
    internal FileSystemDirectoryDirective(IFileSystem fileSystem, ISourceLine sourceLine, string directiveData, int start, int end)
        : base(fileSystem, sourceLine, directiveData, start, end)
    {
        this.DirectiveName = "DIRECTORY";


    }
}

public class DirectivesParser
{
    public static bool IsIncludeDirective(string text)
    {
        var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((DIRECTORY|FILE:))|[:]{1})\s*", Global.SingleLineRegexOptions);
        return regex.IsMatch(text);
    }

    public static bool IsCommentDirective(string text)
    {
        var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((COMMENT|:))|[:]{1})\s*", Global.SingleLineRegexOptions);
        return regex.IsMatch(text);
    }

    public static bool IsRootDirective(string text)
    {
        var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((ROOT))|[:]{1})\s*", Global.SingleLineRegexOptions);
        return regex.IsMatch(text);
    }

    public static bool IsOutputDirective(string text)
    {
        var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((OUTPUT))|[:]{1})\s*", Global.SingleLineRegexOptions);
        return regex.IsMatch(text);
    }

    public static IFileSystemDirective GetFileSystemDirective(ISourceLine sourceLine, IFileSystem fileSystem)
    {
        var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((ROOT|OUTPUT|DIRECTORY|FILE:))|[:]{1})\s*", Global.SingleLineRegexOptions);
        Match m = regex.Match(sourceLine.LineText);
        if (m.Success)
        {
            var directive = m.Groups["directive"];
            var directiveName = directive.Value.ToUpperInvariant().Replace(":", string.Empty).Trim();
            int start = directive.Index;
            int end = start + directive.Length;

            var directiveData = GetDirectiveData(sourceLine.LineText.Substring(end));

            switch (directiveName)
            {
                case "ROOT":
                    return null;
                case "OUTPUT":
                    return null;
                case "FILE":
                    return new FileSystemFileDirective(fileSystem, sourceLine, directiveData, start, end);
                case "DIRECTORY":
                    return new FileSystemDirectoryDirective(fileSystem, sourceLine, directiveData, start, end);
                default:
                    throw new InvalidCastException($"Invalid Directive-Name: [{directiveName}].");
            }
        }
        
        throw new Exception("TODO: need a better error here... but... something's seriously wrong with the INCLUDE directive body... ");
    }

    //public static IDirective GetDirective(ICodeLine codeLine)
    //{
    //    var regex = new Regex(@"^\s*--\s*##\s*(?<directive>((ROOT|OUTPUT|FILEMARKER|VERSION_CHECKER|DIRECTORY|FILE|COMMENT|:))|[:]{1})\s*", Global.SingleLineRegexOptions);
    //    Match m = regex.Match(codeLine.Text);

    //    if (m.Success)
    //    {
    //        var directive = m.Groups["directive"];
    //        var directiveName = directive.Value.ToUpperInvariant().Trim();
    //        int start = directive.Index;
    //        int end = start + directive.Length;

    //        var directiveData = var directiveData = GetDirectiveData(sourceLine.Text.Substring(end));

    //        // TODO: wrap this in a try/catch... 
    //        switch (directiveName)
    //        {
    //            //case ":":  // short-hand/alternative syntax for a comment
    //            //    return new CommentDirective(codeLine, directiveData, start, end);
    //            //case "COMMENT":
    //            //    return new CommentDirective(codeLine, directiveData, start, end);
    //            //case "ROOT":
    //            //    return new RootDirective(codeLine, directiveData, start, end);
    //            //case "OUTPUT":
    //            //    return new OutputDirective(codeLine, directiveData, start, end);
    //            //case "FILE":
    //            //    return new FileSystemFileDirective(fileSystem, codeLine, directiveData, start, end);
    //            //case "DIRECTORY":
    //            //    return new FileSystemDirectoryDirective(fileSystem, codeLine, directiveData, start, end);
    //            default:
    //                throw new InvalidCastException($"Invalid Directive-Name: [{directiveName}].");
    //        }
    //    }

    //    throw new Exception("TODO: need a better error here... but... something's seriously wrong with the directive body... ");
    //}

    private static string GetDirectiveData(string data)
    {
        var output = data.Trim();

        if (output.ToLowerInvariant().Contains("##::"))
        {
            var parts = data.Split("##::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts[0];
        }

        return output;
    }



}