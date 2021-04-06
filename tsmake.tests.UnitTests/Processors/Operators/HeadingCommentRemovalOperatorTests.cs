using System;
using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Core;
using tsmake.Processors.FileProcessors.Operators;

namespace tsmake.Tests.UnitTests.Processors.Operators
{
	[TestFixture]
	public class HeadingCommentRemovalOperatorTests
	{
		[Test]
		public void HeadingCommentRemovalOperator_RemovesMultiLineHeadinComments_WhenPresent()
		{
			var context = new Mock<IBuildContext>();

			string addedFile = FAKE_CODE_FILES.UDF_WITH_HEADER_COMMENTS;

			HeadingCommentRemovalOperator sut = new HeadingCommentRemovalOperator(context.Object);
			string output = sut.Process(addedFile);

			StringAssert.StartsWith("USE [admindb]", output.Trim());
		}

		[Test]
		public void HeadingCommentRemovalOperator_DoesNot_Remove_NonHeadingComments()
		{
			var context = new Mock<IBuildContext>();

			string addedFile = FAKE_CODE_FILES.UDF_WITH_HEADER_COMMENTS_AND_NESTED_COMMENTS;

			HeadingCommentRemovalOperator sut = new HeadingCommentRemovalOperator(context.Object);
			string output = sut.Process(addedFile);

			StringAssert.Contains("HeadingCommentRemovalOperator won't remove them... ", output);
		}

		[Test]
		public void HeadingCommentRemovalOperator_DoesNotRemoveNestedComments_WhenHeadingCommentIsNotPresent()
		{
			var context = new Mock<IBuildContext>();

			string addedFile = FAKE_CODE_FILES.UDF_WITHOUT_HEADER_BUT_WITH_MULTI_LINE_COMMENT;

			HeadingCommentRemovalOperator sut = new HeadingCommentRemovalOperator(context.Object);
			string output = sut.Process(addedFile);

			StringAssert.Contains("nested comment here... ", output);
			StringAssert.Contains("/*", output);

		}

		[Test]
		public void HeadingCommentRemovalOperator_ReportsNonMatched_WhenNoHeadingCommentsEncountered()
		{
			var context = new Mock<IBuildContext>();

			string addedFile = FAKE_CODE_FILES.UDF_WITHOUT_HEADER_BUT_WITH_MULTI_LINE_COMMENT;

			HeadingCommentRemovalOperator sut = new HeadingCommentRemovalOperator(context.Object);
			string output = sut.Process(addedFile);

			Assert.False(sut.Matched);
		}

		[Test]
		public void HeadingCommentRemovalOperator_ReportsMatched_WhenHeadingCommentRemoved()
		{
			var context = new Mock<IBuildContext>();

			string addedFile = FAKE_CODE_FILES.UDF_WITH_HEADER_COMMENTS;

			HeadingCommentRemovalOperator sut = new HeadingCommentRemovalOperator(context.Object);
			string output = sut.Process(addedFile);

			Assert.True(sut.Matched);
		}
	}
}