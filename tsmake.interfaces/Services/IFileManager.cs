
namespace tsmake.Interfaces.Services
{
	public interface IFileManager
	{
		IResourceFile GetFile(string fullPath);

		// void WriteOutputCode(string file, string contents);
		// void CreateMarkerFile(string path, string contents)

		// string CalculateOutputPath(string relativePath);
	}
}