using tsmake.Interfaces.Core;

namespace tsmake.Pipeline
{
	public class BuildVersion : IBuildVersion
	{
		public VersionScheme VersionScheme => throw new System.NotImplementedException();

		public string Major => throw new System.NotImplementedException();

		public string Minor => throw new System.NotImplementedException();

		public string Handle => throw new System.NotImplementedException();

		public string Build => throw new System.NotImplementedException();

		public string VersionSummary => throw new System.NotImplementedException();
	}
}