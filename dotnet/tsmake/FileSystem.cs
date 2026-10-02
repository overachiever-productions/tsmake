using tsmake.data_models;

namespace tsmake;

public interface IFileSystem
{
    string RootDirectory { get; }
    SourceType RootSourceType { get; }

    void SetRootDirectory(string rootDirectory, SourceType sourceType);

    PathType GetPathType(string filePath);
    string TranslatePath(string path);
    
    bool DirectoryExists(string path);
    bool FileExists(string filePath);

    List<string> GetDirectoryFiles(string directory);
    string GetFileContent(string filePath);

    void WriteArtifact(IArtifact artifact);
}

public class FileSystem (string workingDirectory) : IFileSystem
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
        return Directory.Exists(path);
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

    public void WriteArtifact(IArtifact artifact)
    {
        // NOTE: MIGHT pass in PART of an IAssemblerOptions that ... defines
        //      - whether to overwrite existing files OR attempts to use a 'safe' write as outlined below. 


        /*
# ====================================================================================================
# Output:
# ====================================================================================================	
# TODO: move the logic below into IFileSystem ... it needs to be able to handle the backup/write and other (similar) logic. 
# and... honestly, no real reason to CHECK/validate -BuildRoot at this point as ... it might NOT be specified at all. 		
# 	UGH... need to move this into the BuildPipeline ... since -BuildRoot can/will be NULL at this point. 
# 	TODO: 
# 		if -BuildRoot is a FOLDER ... and there are multiple -BuildFiles ... we're fine. 
# 			HOWEVER: the above ONLY works IF each .build.sql file in question has an OUTPUT directive OR a CONFIG-VALUE ... set for the file-name. 
# 		if -BuildRoot is a FILENAME 
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