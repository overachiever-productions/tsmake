using System;
using tsmake.Interfaces.Core;

namespace tsmake.Pipeline
{
	public class BuildVersion : IBuildVersion
	{
		public VersionScheme VersionScheme { get; private set; }
		public int Major { get; private set; }
		public int Minor { get; private set; }
		public int Handle { get; private set; }
		public int Build { get; private set; }
		public string VersionSummary { get; private set; }

		public BuildVersion(VersionScheme scheme, int major, int minor, string versionSummary, int build = -1, DateTime? versionCodeDate = null)
		{
			if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), "Cannot be less than 0");
			if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), "Cannot be less than 0");
			if (string.IsNullOrEmpty(versionSummary)) throw new ArgumentException("Parameter versionSummary cannot be null or empty.");

			this.VersionScheme = scheme;
			this.Major = major;
			this.Minor = minor;

			if (this.VersionScheme == VersionScheme.FourPart)
				this.Handle = this.GetHandle(versionCodeDate);

			this.Build = this.GetBuildNumber(build);

			this.VersionSummary = versionSummary;
		}

		private int GetHandle(DateTime? versionCodeDate)
		{
			DateTime start = new DateTime(2011, 6, 15);
			if (versionCodeDate.HasValue)
			{
				start = new DateTime(versionCodeDate.Value.Year, versionCodeDate.Value.Month, versionCodeDate.Value.Day);
			}

			int output = (DateTime.Now - start).Days;
			return output;
		}

		private int GetBuildNumber(int build)
		{
			if (build != -1)
				return build;

			return 0;
		}

		public override string ToString()
		{
			return this.VersionScheme == VersionScheme.FourPart ? $"{Major}.{Minor}.{Handle}.{Build}" : $"{Major}.{Minor}.{Build}";
		}
	}
}