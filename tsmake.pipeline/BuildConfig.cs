using System;
using Microsoft.Extensions.Configuration;
using tsmake.Interfaces.Configuration;

namespace tsmake.Pipeline
{
	public class BuildConfig : IBuildConfig
	{
		public VersionScheme VersionScheme { get; }
		public DateTime VersionCodeDate { get; }
		public string BuildMarkerTemplatePath { get; }
		public string BuildOutputPath { get; }
		public string BuildRoot { get; }
		public string CopyrightText { get; }
		public string ProjectInfoText { get; }

		public BuildConfig(IConfigurationRoot configRoot)
		{
			this.VersionScheme = Enum.Parse<VersionScheme>(configRoot["VersionScheme"]);
			this.VersionCodeDate = DateTime.Parse(configRoot["VersionCodeDate"]);

			this.BuildMarkerTemplatePath = configRoot["BuildMarkerTemplatePath"];
			this.BuildOutputPath = configRoot["BuildOutputPath"];
			this.BuildRoot = configRoot["BuildRoot"];
			this.CopyrightText = configRoot["CopyrightText"];
			this.ProjectInfoText = configRoot["ProjectInfoText"];
		}
	}
}