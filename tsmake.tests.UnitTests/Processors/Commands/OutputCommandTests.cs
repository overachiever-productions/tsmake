using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Core;
using tsmake.Processors.BuildProcessors.Commands;

namespace tsmake.Tests.UnitTests.Processors.Commands
{
	[TestFixture]
	public class OutputCommandTests
	{
		[Test]
		public void OutputCommand_SetsOutputPath_OutputPathWhenPresent()
		{
			var context = new Mock<IBuildContext>();

			string input = @"--##OUTPUT: \\Deployment
--##NOTE: This is a build file only (i.e., it stores upgade/install directives + place-holders for code to drop into admindb, etc.)";

			OutputCommand sut = new OutputCommand(context.Object);
			string output = sut.Process(input);

			context.Verify(x => x.SetOutputPath(It.IsAny<string>()), Times.Once());
		}

		[Test]
		public void OutputCommand_CorrectlyExtracts_OutputPathWhenPresent()
		{
			var context = new Mock<IBuildContext>();

			string input = @"--##OUTPUT: \\Deployment\dda_latest.sql
--##NOTE: This is a build file only (i.e., it stores upgade/install directives + place-holders for code to drop into admindb, etc.)";

			OutputCommand sut = new OutputCommand(context.Object);
			string output = sut.Process(input);

			context.Verify(x => x.SetOutputPath(@"\\Deployment\dda_latest.sql"), Times.Once);
		}

		[Test]
		public void OutputCommand_Replaces_OutputLine()
		{
			var context = new Mock<IBuildContext>();

			string input = @"--##OUTPUT: \\Deployment
--##NOTE: This is a build file only (i.e., it stores upgade/install directives + place-holders for code to drop into admindb, etc.)";

			OutputCommand sut = new OutputCommand(context.Object);
			string output = sut.Process(input);

			StringAssert.AreEqualIgnoringCase(@"--##NOTE: This is a build file only (i.e., it stores upgade/install directives + place-holders for code to drop into admindb, etc.)", output);
		}
	}
}