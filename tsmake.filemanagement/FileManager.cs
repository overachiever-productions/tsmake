using System.IO;
using System.Text;
using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Services;

namespace tsmake.FileManagement
{
	public class FileManager : IFileManager
	{
		private string _rootDirectory;

		public FileManager(string rootDirectory)
		{
			this._rootDirectory = rootDirectory;
		}

		public IResourceFile GetFile(string fullPath)
		{
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException($"Build file not found at {fullPath}.");
			}

			StreamReader reader = new StreamReader(fullPath);
			string contents = reader.ReadToEnd();

			ResourceFile output = new ResourceFile(fullPath, contents);
			return output;
		}
	}
}