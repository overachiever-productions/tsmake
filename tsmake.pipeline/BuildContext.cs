using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Services;

namespace tsmake.Pipeline
{
	public class BuildContext : IBuildContext
	{
		public IBuildConfig Configuration { get; private set; }
		public IBuildVersion BuildVersion { get; private set; }
		public IFileManager FileManager { get; private set; }
		public IBuildFile BuildFile { get; private set; }
		public string ProjectRoot { get; private set; }
		public string OutputPath { get; private set; }

		public BuildContext(IBuildConfig config, IBuildVersion version, IBuildFile buildFile, IFileManager fileManager)
		{
			this.Configuration = config;
			this.BuildVersion = version;
			this.FileManager = fileManager;
			this.BuildFile = buildFile;
		}

		public void SetOutputPath(string outputPath)
		{
			this.OutputPath = outputPath;
		}

		public void SetProjectRoot(string projectRoot)
		{
			this.ProjectRoot = projectRoot;
		}
	}
}