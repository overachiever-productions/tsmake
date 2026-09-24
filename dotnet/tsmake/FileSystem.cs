using tsmake.data_models;

namespace tsmake;

public interface IFileSystem
{
    string WorkingDirectory { get; }
    string RootDirectory { get; }
    void SetRootDirectory(string rootDirectory);

    string TranslatePath(string path, PathType pathType);
    List<string> GetDirectoryFiles(string directory);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    PathType GetPathType(string filePath, bool strict = false);
    bool IsValidFilePath(string filePath);
    string GetFileContent(string filePath);

    void WriteArtifact(IArtifact artifact);
}

public class FileSystem (string workingDirectory) : IFileSystem
{
    public string WorkingDirectory { get; } = workingDirectory;
    public string RootDirectory { get; private set; } = null!;

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
                throw new Exception($"Invalid Path Type Specified: [{pathType}].");
        }
    }

    public List<string> GetDirectoryFiles(string directory)
    {
        throw new NotImplementedException();
        // not in v2025 (i.e., tsmake2 ... but I probably had logic for htis in 'v1'. 
    }

    public bool DirectoryExists(string path)
    {
        return FileOrDirectoryExists(path);
    }

    public bool FileExists(string path)
    {
        // NOTE:  This func attempts to white-list known valid patterns - anything else is going to drop-out as FALSE.

        // Absolute Path - local machine.
        if (Regex.IsMatch(path, @"^[A-Za-z]{1}:\\", Global.SingleLineRegexOptions))
        {
            if (FileOrDirectoryExists(path))
                return true;

            if (PathContainsIllegalCharacters(path))
                return false;

            return true;
        }

        // Absolute Path - but against a UNC share:
        if (path.StartsWith(@"//"))
        {
            if (FileOrDirectoryExists(path))
                return true;

            if (PathContainsIllegalCharacters(path))
                return false;

            return true;
        }

        // Relative Path - but from the current directory (i.e., no / in the path):
        if (!path.ToLowerInvariant().Contains("/"))
        {
            if (PathContainsIllegalCharacters(path))
                return false;

            return true;
        }

        // Relative Path - but 'up' from current directory. 
        if (path.StartsWith(@"../"))
        {
            if (PathContainsIllegalCharacters(path))
                return false;

            return true;
        }

        // Relative Path - but in child directory: 
        if (path.ToLowerInvariant().Contains("/"))
        {
            if (PathContainsIllegalCharacters(path))
                return false;

            return true;
        }

        return false;
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
        return FileOrDirectoryExists(filePath);
    }

    public string GetFileContent(string filePath)
    {
        return File.ReadAllText(filePath);
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

    public static bool FileOrDirectoryExists(string path)
    {
        return (Directory.Exists(path) || File.Exists(path));
    }

    public static bool PathContainsIllegalCharacters(string path)
    {
        // MVP: honestly, stop caring about this so much. Either a file/path can be found, or the OS will throw an error. that's on the USER. 

        // See: https://stackoverflow.com/a/31976060/11191 

        // TODO: need to key this against current OS (i.e., Environment.Platform/etc.)
        if (Regex.IsMatch(path, @"(\<|\>|""|\||\?|\*)+", Global.SingleLineRegexOptions))
            return true;

        // TODO: ARGUABLY, could/should look for additional problems like: NULL byte, ASCII 0 - 31, reserved filenames (windows), and other rules

        return false;
    }

    public void WriteArtifact(IArtifact artifact)
    {
        // NOTE: MIGHT pass in PART of an IAssemblerOptions that ... defines
        //      - whether to overwrite existing files OR attempts to use a 'safe' write as outlined below. 


        /*
# ====================================================================================================
# Output:
# ====================================================================================================	
# TODO: move the logic below into IFileSystem ... it needs to be able to handle the backup/write and other (similar) logic. 
# and... honestly, no real reason to CHECK/validate -OutputPath at this point as ... it might NOT be specified at all. 		
# 	UGH... need to move this into the BuildPipeline ... since -OutputPath can/will be NULL at this point. 
# 	TODO: 
# 		if -OutputPath is a FOLDER ... and there are multiple -BuildFiles ... we're fine. 
# 			HOWEVER: the above ONLY works IF each .build.sql file in question has an OUTPUT directive OR a CONFIG-VALUE ... set for the file-name. 
# 		if -OutputPath is a FILENAME 
# 			the INTENTION of a BUILD is to ... replace whatever is already in place - i.e., I do this all the time with admindb_latest.sql .. 
# 				I just overwrite it. 
# 			So, I'm not sure that there's any justification for:
# 				- THROW if the file exists. 
# 				- Requiring something like -Force 
# 				- Prompting the user to overwrite. 
# 			BUT, FEATURE-CREEP:
# 				I can see that if a file already exists...
# 					 i rename it to xxxx.sql.backup. 
# 				IF the build fails ... 
# 					i could revert? 
# 						or tell users there's a copy.
# 				IF the build succeeds, then delete .backup... 
*/

        // Implementation for writing the artifact

        // i.e., attempt to write the file - using the logic above... 
        //  and if there are failures, ... 
        //     - catch the exception and hand it off/into the IArtifact for reporting.
    }
}