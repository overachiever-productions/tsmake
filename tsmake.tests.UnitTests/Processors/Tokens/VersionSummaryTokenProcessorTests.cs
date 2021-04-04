using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Core;
using tsmake.Processors.GlobalProcessors.Tokens;

namespace tsmake.Tests.UnitTests.Processors.Tokens
{
	[TestFixture]
	public class VersionSummaryTokenProcessorTests
	{
		[Test]
		public void VersionSummaryTokenProcessor_ReplacesVersionSummaryToken_FromBuildVersion()
		{
			var version = new Mock<IBuildVersion>();
			var context = new Mock<IBuildContext>();

			string summary = "Bug Fixes + documentation improvements.";

			version.Setup(x => x.VersionSummary).Returns(summary);
			context.Setup(x => x.BuildVersion).Returns(version.Object);

			string input = "--{{##SUMMARY}}  ";
			VersionSummaryTokenProcessor sut = new VersionSummaryTokenProcessor(context.Object);

			string output = sut.Process(input);

			StringAssert.Contains(summary, output);
		}
	}
}