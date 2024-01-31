using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Services;

namespace tsmake.Interfaces.Core
{
	public interface IBuildContext
	{
		public IBuildConfig Configuration { get; }
		public IBuildVersion BuildVersion { get; }
		public IFileManager FileManager { get; }
		public IBuildFile BuildFile { get; }

		public string ProjectRoot { get;  }
		public string OutputPath { get; }

		public void SetOutputPath(string outputPath);
		public void SetProjectRoot(string projectRoot);
	}
}