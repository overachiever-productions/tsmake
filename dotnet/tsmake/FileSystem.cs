namespace tsmake;

public interface IFileWriteResult
{
    bool Success { get; }
    string FilePath { get; }
    DateTime Written { get; }
    int FileSizeKb { get; }
    Exception? Exception { get; }
}

public class FileWriteResult(string filePath, DateTime written, int fileSizeKb) : IFileWriteResult
{
    public string FilePath { get; } = filePath;
    public bool Success => this.Exception is null;
    public DateTime Written { get; } = written;
    public int FileSizeKb { get; internal set; } = fileSizeKb;
    public Exception? Exception { get; internal set; }
}

public interface IFileSystem
{
    string RootDirectory { get; }
    SourceType RootSourceType { get; }

    void SetRootDirectory(string rootDirectory, SourceType sourceType);

    PathType GetPathType(string filePath);
    string TranslatePath(string path);
    
    bool DirectoryExists(string path);
    bool FileExists(string filePath);

    List<(string FilePath, DateTime TimeStamp)> GetDirectoryFiles(string directory);
    string GetFileContent(string filePath);

    //void WriteArtifact(IArtifact artifact);
    IFileWriteResult WriteFile(string filePath, string contents); //, bool overwrite = true);
}

public class FileSystem(string workingDirectory) : IFileSystem
{
    public string RootDirectory { get; private set; } = workingDirectory;
    public SourceType RootSourceType { get; private set; } = SourceType.Convention;

    public void SetRootDirectory(string rootDirectory, SourceType sourceType)
    {
        this.RootDirectory = rootDirectory;
        this.RootSourceType = sourceType;
    }

    public PathType GetPathType(string filePath)
    {
        return filePath.GetPathType();
    }

    public string TranslatePath(string path)
    {
        if (this.RootDirectory == string.Empty)
            throw new Exception("tsmake Workflow Exception: ROOT Directory has not been set.");

        PathType pathType = GetPathType(path);
        switch (pathType)
        {
            case PathType.Absolute:
                return path;
            case PathType.Relative:
                return CollapsePath(this.RootDirectory, path);
            case PathType.Rooted:
                return CollapsePath(this.RootDirectory, path.Substring(2));     // strip ONLY the leading ~\ (other '~' chars are legal in names).
            default:
                throw new Exception($"Invalid Path Type Specified: [{pathType}].");
        }
    }

    public List<(string FilePath, DateTime TimeStamp)> GetDirectoryFiles(string directory)
    {
        string[] files = Directory.GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly);

        return files
            .Select(f => (FilePath: f, TimeStamp: File.GetLastWriteTime(f)))
            .ToList();
    }

    public bool DirectoryExists(string path)
    {
        string pathDirectory = System.IO.Path.GetDirectoryName(path) ?? path;

        return Directory.Exists(pathDirectory);
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    //public static bool FileOrDirectoryExists(string path)
    //{
    //    return (Directory.Exists(path) || File.Exists(path));
    //}

    public string GetFileContent(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public IFileWriteResult WriteFile(string filePath, string contents) //, bool overwrite = true)
    {
        var result = new FileWriteResult(filePath, DateTime.Now, 0);
        try
        {
            File.WriteAllText(filePath, contents);
            int fileSizeKb = (int)(new FileInfo(filePath).Length / 1024);
            result.FileSizeKb = fileSizeKb;
        }
        catch (Exception ex)
        {
            result.Exception = ex;
        }

        return result;
    }

    private static string CollapsePath(string rootPath, string addedPath)
    {
        string newPath = rootPath;
        string newDirective = addedPath;

        while (newDirective.StartsWith(@"..\"))
        {
            newPath = Directory.GetParent(newPath)!.FullName;
            newDirective = newDirective.Substring(3);
        }

        string output = Path.Join(newPath, newDirective);
        return output;
    }
}