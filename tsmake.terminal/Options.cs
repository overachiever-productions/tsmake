using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommandLine;

namespace tsmake.terminal
{
	[ExcludeFromCodeCoverage]
	public class Options
	{
		[Option('b', "BuildFile", Required = true, HelpText = "Path to the build-file or manifest of scripts + directives to process.")]
		public string BuildFile { get; set; }

		[Option('v', "MajorMinor", Required = true, HelpText = "Major + Minor version details in string form as Major.Minor - e.g., 4.5.")]
		public string MajorMinor { get; set; }

		[Option('s', "VersionSummary", Required = true, HelpText = "Text summary/overview of current version.")]
		public string VersionSummary { get; set; }

		[Option('x', "BuildNumber", Default = 1, Required = false, HelpText = "Specific build number for the currently specified build/version.")]
		public int BuildNumber { get; set; }

		public static void ProcessParserErrors(IEnumerable<Error> errors)
		{
			if (errors.IsVersion())
			{
				Console.WriteLine("Version Request");
				return;
			}

			if (errors.IsHelp())
			{
				Console.WriteLine("Help Request");
				return;
			}
			Console.WriteLine("Parser Fail");
		}

		public int GetMajorVersion()
		{
			string[] parts = this.MajorMinor.Split(".");
			int output = int.TryParse(parts[0], out output) ? output : 0;

			return output;
		}

		public int GetMinorVersion()
		{
			string[] parts = this.MajorMinor.Split(".");
			int output = int.TryParse(parts[1], out output) ? output : 0;

			return output;
		}
	}
}