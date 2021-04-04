using tsmake.Interfaces.Enums;

namespace tsmake.Interfaces.Core
{
	public interface IBuildVersion
	{
		public VersionScheme VersionScheme { get; }

		public string Major { get; }
		public string Minor { get; }
		public string Handle { get; } // i.e., S4's build number, etc. and... might want an IHandleGenerator or something that takes in ... a date and/or some sort of of other logic... and spits out a 3-6 digit whatzit... 
		public string Build { get; }

		public string VersionSummary { get; }
	}
}