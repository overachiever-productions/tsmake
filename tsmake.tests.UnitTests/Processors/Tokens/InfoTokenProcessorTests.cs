using Castle.DynamicProxy.Generators.Emitters;
using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Core;
using tsmake.Processors.GlobalProcessors.Tokens;

namespace tsmake.Tests.UnitTests.Processors.Tokens
{
	[TestFixture]
	public class InfoTokenProcessorTests
	{
		[Test]
		public void InfoTokenProcessor_ReplacesInfoToken_FromConfigValue()
		{
			var config = new Mock<IBuildConfig>();
			var context = new Mock<IBuildContext>();

			string infoText = "For more info see https://github.com/whatever";
			config.Setup(x => x.ProjectInfoText).Returns(infoText);
			context.Setup(x => x.BuildConfiguration).Returns(config.Object);

			string input = "-- {{##INFO}}    ";
			InfoTokenProcessor sut = new InfoTokenProcessor(context.Object);

			string output = sut.Process(input);

			StringAssert.Contains(infoText, output);
		}
	}
}