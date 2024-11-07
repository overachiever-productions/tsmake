namespace tsmake;

public interface IFileSystem
{
    string WorkingDirectory { get; }
    string RootDirectory { get; }
    void SetRootDirectory(string rootDirectory);

    string TranslatePath(string path, PathType pathType);
    //List<string> GetDirectoryFiles(string directory, RecursionOption recursion);
    //bool DirectoryExists(string path);
    //bool FileExists(string path);
    //List<string> GetFileLines(string filePath);
    PathType GetPathType(string filePath, bool strict = false);
    bool IsValidFilePath(string filePath);
    List<string> GetFileLines(string filePath);
}

public class FileSystem(string workingDirectory) : IFileSystem
{
    public string WorkingDirectory { get; } = workingDirectory;
    public string RootDirectory { get; private set; }

    public void SetRootDirectory(string rootDirectory)
    {
        this.RootDirectory = rootDirectory;
    }

    public string TranslatePath(string path, PathType pathType)
    {
        if (this.RootDirectory == string.Empty)
            throw new Exception("tsmake Workflow Exception: ROOTDirectory has not been set.");

        switch (pathType)
        {
            case PathType.Absolute:
                return path;
            case PathType.Relative:
                return CollapsePath(this.WorkingDirectory, path);
            case PathType.Rooted:
                return CollapsePath(this.RootDirectory, path.Replace(@"\\\", ""));
            default:
                throw new InvalidDataException($"Invalid Path Type Specified: [{pathType}].");
        }
    }

    public PathType GetPathType(string filePath, bool strict = false)
    {
        if (strict)
        {
            if (!this.IsValidFilePath(filePath))
                throw new InvalidOperationException("tsmake Workflow Exception: Can not evaluate PathType when Path is deemed invalid.");
        }

        if (filePath.StartsWith(@"\\\"))
            return PathType.Rooted;

        // Absolute - Local File
        if (Regex.IsMatch(filePath, @"^[A-Za-z]{1}:\\", Global.SingleLineRegexOptions))
            return PathType.Absolute;

        // Absolute - UNC Share
        if (filePath.StartsWith("//"))
            return PathType.Absolute;

        return PathType.Relative;
    }

    public bool IsValidFilePath(string filePath)
    {
        // NOTE:  This func attempts to white-list known valid patterns - anything else is going to drop-out as FALSE.

        // Absolute Path - local machine.
        if (Regex.IsMatch(filePath, @"^[A-Za-z]{1}:\\", Global.SingleLineRegexOptions))
        {
            if (FileOrDirectoryExists(filePath))
                return true;

            if (PathContainsIllegalCharacters(filePath))
                return false;

            return true;
        }

        // Absolute Path - but against a UNC share:
        if (filePath.StartsWith(@"//"))
        {
            if (FileOrDirectoryExists(filePath))
                return true;

            if (PathContainsIllegalCharacters(filePath))
                return false;

            return true;
        }

        // Relative Path - but from the current directory (i.e., no / in the path):
        if (!filePath.ToLowerInvariant().Contains("/"))
        {
            if (PathContainsIllegalCharacters(filePath))
                return false;

            return true;
        }

        // Relative Path - but 'up' from current directory. 
        if (filePath.StartsWith(@"../"))
        {
            if (PathContainsIllegalCharacters(filePath))
                return false;

            return true;
        }

        // Relative Path - but in child directory: 
        if (filePath.ToLowerInvariant().Contains("/"))
        {
            if (PathContainsIllegalCharacters(filePath))
                return false;

            return true;
        }

        return false;
    }

    public List<string> GetFileLines(string filePath)
    {
        return File.ReadAllLines(filePath).ToList();
    }

    public static bool FileOrDirectoryExists(string path)
    {
        return (Directory.Exists(path) || File.Exists(path));
    }

    public static bool PathContainsIllegalCharacters(string path)
    {
        // See: https://stackoverflow.com/a/31976060/11191 

        // TODO: need to key this against current OS (i.e., Environment.Platform/etc.)
        if (Regex.IsMatch(path, @"(\<|\>|""|\||\?|\*)+", Global.SingleLineRegexOptions))
            return true;

        // TODO: ARGUABLY, could/should look for additional problems like: NULL byte, ASCII 0 - 31, reserved filenames (windows), and other rules

        return false;
    }

    private static string CollapsePath(string rootPath, string addedPath)
    {
        string newPath = rootPath;
        string newDirective = addedPath;

        while (newDirective.StartsWith(@"..\"))
        {
            newPath = Directory.GetParent(newPath).FullName;
            newDirective = newDirective.Substring(3);
        }

        string output = Path.Join(newPath, newDirective);
        return output;
    }
}