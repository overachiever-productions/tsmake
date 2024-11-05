namespace tsmake;

// TODO: change the name of this to a BuildManifest... 1) I've already got a -BuildFile in Posh...
//      (don't need another one) and 2) I'm already using 'manifest' for the lines. 

public interface IManifestLine
{
    int LineNumber { get; }
    int Depth { get; }
    string FileName { get; }  // name of the current file. 
    Stack<string> Stack { get; }
    string LineText { get; }
}

public class ManifestLine(int lineNumber, string fileName, string text, int depth, Stack<string> stack) : IManifestLine
{
    public int LineNumber { get; } = lineNumber;
    public int Depth { get; } = depth;
    public string FileName { get; } = fileName;
    public Stack<string> Stack { get; } = stack;
    public string LineText { get; } = text;
}

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

public class SimpleFileSystem(string workingDirectory) : IFileSystem
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

// TODO: create an interface... (for testing)
public class Manifest(IFileSystem fileSystem)
{
    private Stack<string> Stack = new Stack<string>();
    private IFileSystem FileSystem = fileSystem;
    
    public RootDirective RootDirective { get; private set; } 
    public OutputDirective OutputDirective { get; private set; }

    public List<IManifestLine> ManifestLines { get; } = new List<IManifestLine>();

    public void LoadContents(string filePath, int depth = 0)
    {
        if (depth == 0)
        {
            // TODO: make sure the filePath is good and such. 
            // and... maybe I need to do this for 'deeper' recursions ... but I don't ... think? so? 
        }

        List<string> rawCodeLines = this.FileSystem.GetFileLines(filePath);

        this.ProcessCoreDirectives(rawCodeLines, filePath);
        if (this.RootDirective != null)
            this.FileSystem.SetRootDirectory(this.RootDirective.AbsolutePath);
        else
            this.FileSystem.SetRootDirectory(this.FileSystem.WorkingDirectory);



        int lineNumber = 0;

// TODO: ?? Add filePath to a STACK of strings ... commensurate with DEPTH... 

        foreach (string rawCodeLine in rawCodeLines)
        {
            // ALWAYS increment the line# - otherwise, we LOSE original line#s for reporting on problems/errors/etc. 
            lineNumber++; 

            if (DirectivesParser.IsCommentDirective(rawCodeLine))
                continue;

            // TODO: if it's a ROOT or OUTPUT directive... continue as well. i.e., no need to dump those into the output. 
            
            var currentLine = new ManifestLine(lineNumber, filePath, rawCodeLine, depth, new Stack<string>(this.Stack));

            if (DirectivesParser.IsIncludeDirective(rawCodeLine))
            {
                var include = DirectivesParser.GetFileSystemDirective(currentLine, this.FileSystem);

                foreach (var child in include.GetChildren())
                {
                    var manifestLines = RecurseSubFile(child, depth + 1);
                    foreach (var line in manifestLines)
                    {
                        // if it's illegal... ignore or throw... 

                        // if it's a comment ... don't add. 

                        // otherwise:
                        this.ManifestLines.Add(line);
                    }

                    // PICKUP/NEXT:
                    //      note, for EACH FILE... get the CONTENTS and ... then stream them OUT into the results.... 
                    //          i.e., recursively... 

                    // NEW FAKE: 
                    //string fullPath = child;
                    //this.ManifestLines.Add(new ManifestLine(100 + lineNumber, child, fullPath, depth + 1, new Stack<string>(this.Stack)));


                    // FAKE:
                    //string fullPath = this.FileSystem.TranslatePath(include.Path, include.PathType);
                    //string content = $"PATH: [{include.Path}]; PATH-TYPE: [{include.PathType}]; ROOT-PATH: [{this.FileSystem.RootDirectory}]; FULL PATH: [{fullPath}]";
                    //var line1 = new ManifestLine(100 + lineNumber, child, content, 1, new Stack<string>());
                    //this.ManifestLines.Add(line1);
                }

            }
            else
                this.ManifestLines.Add(currentLine);
        }
    }

    private List<IManifestLine> RecurseSubFile(string fullFilePath, int depth)
    {
        // so... stack will be pushed/popped and then 'copied' into each ... ManifestLine... 

        try
        {
            this.Stack.Push(fullFilePath);


            var output = new List<IManifestLine>();

            List<string> rawCodeLines = this.FileSystem.GetFileLines(fullFilePath);

            int lineNumber = 0;
            foreach (var line in rawCodeLines)
            {
                lineNumber++;
                // if it's an include... then recursively call back into this func: 

                // otherwise...
                output.Add(new ManifestLine(lineNumber, fullFilePath, line, depth, new Stack<string>(this.Stack)));
            }

            return output;
        }
        catch 
        {
            throw;
        }
        finally
        {
            this.Stack.Pop();
        }
    }

    private void ProcessCoreDirectives(List<string> rawCodeLines, string filePath)
    {
        bool rooted = false;
        bool outputed = false;

        int lineNumber = 0;
        foreach (string line in rawCodeLines)
        {
            lineNumber++; // ALWAYS increments... 

            if (DirectivesParser.IsRootDirective(line))
            {
                var manifestLine = new ManifestLine(lineNumber, filePath, line, 0, new Stack<string>(this.Stack));
                this.RootDirective = (RootDirective)DirectivesParser.GetFileSystemDirective(manifestLine, this.FileSystem);

                rooted = true;
            }

            if (DirectivesParser.IsOutputDirective(line))
            {

                outputed = true;
            }

            if (rooted && outputed)
                return;

        }
    }
}
