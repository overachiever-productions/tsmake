using System.Collections.Generic;
using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Services;

namespace tsmake.Pipeline
{
	public class BuildFile : IBuildFile
	{
		private IFileManager _fileManager;
		
		public string RootPath { get; private set; }
		public List<IResourceFile> IncludedFiles { get; private set; }

		public BuildFile(IFileManager fileManager)
		{
			this._fileManager = fileManager;
		}

		public string LoadBuildFile(string buildFilePath)
		{
			IResourceFile actualBuildResourceFile = this._fileManager.GetFile(buildFilePath);

			// regex to find the OUTPUT directive: 
			Regex r = new Regex(@"--\s*##OUTPUT:.*\n{1}", RegexOptions.CultureInvariant | RegexOptions.Multiline);
			Match m = r.Match(actualBuildResourceFile.FileContents);
			if (m.Success)
			{
				string command = m.Value;
				string outputPath = command.Split(":")[1].Trim();

				return outputPath;
			}
			
			throw new ConfigurationException(
				"Invalid Build configuration File. tsmake directive for project/build output (--##OUTPUT:) not found.");
		}

		public void AddIncludedFile(IResourceFile path)
		{
			throw new System.NotImplementedException();
		}
	}
}