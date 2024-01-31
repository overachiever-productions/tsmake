using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Core;
using tsmake.Processors.GlobalProcessors.Tokens;

namespace tsmake.Tests.UnitTests.Processors.Tokens
{
	[TestFixture]
	public class CopyrightTokenProcessorTests
	{
		[Test]
		public void CopyrightTokenProcessor_ReplacesCopyrightToken_FromConfigValue()
		{
			var config = new Mock<IBuildConfig>();
			var context = new Mock<IBuildContext>();

			string configCopyrightText = "2019 - by me!";
			config.Setup(x => x.CopyrightText).Returns(configCopyrightText);
			context.Setup(x => x.Configuration).Returns(config.Object);

			string input = "this is a string that has a {{##COPYRIGHT}} token in it.";
			CopyrightTokenProcessor sut = new CopyrightTokenProcessor(context.Object);

			string output = sut.Process(input);

			StringAssert.Contains(configCopyrightText, output);
		}
	}
}