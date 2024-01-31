using System.Collections.Generic;
using tsmake.Interfaces.Services;

namespace tsmake.Interfaces.Core
{
	// REFACTOR: rename to ... IBuildManifest ... or IBuild? 
	public interface IBuildFile
	{
		public string RootPath { get; }
		List<IResourceFile> IncludedFiles { get; }

		string LoadBuildFile(string buildFilePath);

		void AddIncludedFile(IResourceFile path);
	}
}