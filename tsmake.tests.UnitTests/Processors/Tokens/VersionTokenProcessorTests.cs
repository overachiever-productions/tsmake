using System;
using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Core;
using tsmake.Processors.GlobalProcessors.Tokens;

namespace tsmake.Tests.UnitTests.Processors.Tokens
{
	[TestFixture]
	public class VersionTokenProcessorTests
	{
		[Test]
		public void VersionTokenProcessor_ReplacesVersionToken_FromBuildVersion()
		{
			var version = new Mock<IBuildVersion>();
			var context = new Mock<IBuildContext>();

			string v = "9.8.3456.2";
			version.Setup(x => x.ToString()).Returns(v);
			context.Setup(x => x.BuildVersion).Returns(version.Object);

			string input = "--{{##VERSION}}  ";
			VersionTokenProcessor sut = new VersionTokenProcessor(context.Object);

			string output = sut.Process(input);

			StringAssert.Contains(v, output);
		}
	}
}