using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using tsmake.Pipeline;

namespace tsmake.Tests.UnitTests.Services.Pipeline
{
	[TestFixture]
	public class BuildConfigTests
	{
		[Test]
		public void BuildConfig_Assigns_VersionScheme_FromConfigSource()
		{
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>() { {"VersionScheme", "SemVer"} });

			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(VersionScheme.SemVer, sut.VersionScheme);
		}

		[Test]
		public void BuildConfig_Assigns_VersionCodeDate_FromConfigSource()
		{
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>() { { "VersionCodeDate", "2012-12-18" } });
			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(2012, sut.VersionCodeDate.Year);
			Assert.AreEqual(12, sut.VersionCodeDate.Month);
			Assert.AreEqual(18, sut.VersionCodeDate.Day);
		}
		
		[Test]
		public void BuildConfig_Assigns_BuildMarkerTemplatePath_FromConfigSource()
		{
			var buildMarkerTemplatePath = @"D:\Repositories\dda\templates\marker.md";
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>()
				{{"BuildMarkerTemplatePath", buildMarkerTemplatePath}});


			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(buildMarkerTemplatePath, sut.BuildMarkerTemplatePath);
		}

		[Test]
		public void BuildConfig_Assigns_BuildMakerOutputPath_FromConfigSource()
		{
			string buildOutputPath = @"D:\Repositories\dda\deployment\dda_latest.sql";

			var configRoot = new FakeConfigRoot(new Dictionary<string, string>()
				{{"BuildOutputPath", buildOutputPath}});

			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(buildOutputPath, sut.BuildOutputPath);
		}

		[Test]
		public void BuildConfig_Assigns_BuildRoot_FromConfigSource()
		{
			string root = @"D:\Repositories\dda";
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>() {{"BuildRoot", root}});

			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(root, sut.BuildRoot);
		}

		[Test]
		public void BuildConfig_Assigns_CopyrightText_FromConfigSource()
		{
			string copyrightText = "Copyright Text Here... ";
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>() { { "CopyrightText", copyrightText } });

			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(copyrightText, sut.CopyrightText);
		}

		[Test]
		public void BuildConfig_Assigns_ProjectInfoText_FromConfigSource()
		{
			string projectText = "For more info, see https://www.xyz.com";
			var configRoot = new FakeConfigRoot(new Dictionary<string, string>() {{"ProjectInfoText", projectText}});

			BuildConfig sut = new BuildConfig(configRoot);

			Assert.AreEqual(projectText, sut.ProjectInfoText);
		}
	}
}