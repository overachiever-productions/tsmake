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

			string input = "--##NOTE: this is a line that should be removed...";
			NoteCommand sut = new NoteCommand(context.Object);

			string output = sut.Process(input);

			StringAssert.AreEqualIgnoringCase(string.Empty, output);

		}

		[Test]
		public void NoteCommand_Replaces_MultipleNoteLines_When_Present()
		{
			throw new NotImplementedException();
		}
	}
}