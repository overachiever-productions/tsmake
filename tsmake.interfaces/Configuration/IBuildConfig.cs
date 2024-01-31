using System;

namespace tsmake.Interfaces.Configuration
{
	public interface IBuildConfig
	{
		VersionScheme VersionScheme { get; }
		DateTime VersionCodeDate { get; }
		
		string BuildMarkerTemplatePath { get; }
		string BuildOutputPath { get; }
		string BuildRoot { get; }

		// BuildLogPath... 

		// might break these out into a distinct config in the future: 
		string CopyrightText { get; }
		string ProjectInfoText { get; }
	}
}