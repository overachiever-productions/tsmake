using System;
using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Core;
using tsmake.Processors.GlobalProcessors.Commands;

namespace tsmake.Tests.UnitTests.Processors.Commands
{
	[TestFixture]
	public class NoteCommandTests
	{
		[Test]
		public void NoteCommand_Ignores_NonNoteLines()
		{
			var context = new Mock<IBuildContext>();

			string input = "DECLARE @tableName sysname, @schema sysname;";
			NoteCommand sut = new NoteCommand(context.Object);

			string output = sut.Process(input);

			StringAssert.AreEqualIgnoringCase(input, output);
		}

		[Test]
		public void NoteCommand_Replaces_SingleNoteLine_With_EmptyStrings()
		{
			var context = new Mock<IBuildContext>();
			
			string input = "--##NOTE: This is a line to replace..." + Environment.NewLine;
			NoteCommand sut = new NoteCommand(context.Object);

			string output = sut.Process(input);

			StringAssert.AreEqualIgnoringCase(string.Empty, output);
		}

		[Test]
		public void NoteCOmmand_Replaces_EntireNoteLine_WhenFound()
		{
			var context = new Mock<IBuildContext>();

			string input = FAKE_BUILD_SCRIPTS.MISC.MULTI_LINE_WITH_NOTE;
			NoteCommand sut = new NoteCommand(context.Object);
			string output = sut.Process(input);

			StringAssert.DoesNotContain("--##NOTE:", output);
			StringAssert.DoesNotContain(" should be captured and ... replaced.", output);
		}

		[Test]
		public void NoteCommand_Replaces_MultipleNoteLines_When_Present()
		{
			var context = new Mock<IBuildContext>();

			string input = FAKE_BUILD_SCRIPTS.S4_SAMPLE.FILE_START;
			NoteCommand sut = new NoteCommand(context.Object);
			string output = sut.Process(input);

			StringAssert.DoesNotContain("--##NOTE:", output);
			StringAssert.DoesNotContain("This is a build file only", output);
			StringAssert.DoesNotContain("This is another note... it, too, should be ", output);
		}
	}
}