using tsmake.models;

namespace tsmake.tests.unit_tests.file_tests;

// REFACTOR: this isn't just buildFile tests ... in fact, these are almost ALL 

[TestFixture]
public class BuildFileTests
{
    #region Calibration and Debugging Strings
    public const string DEBUGGING_LINES = @"12345678
CDEF
0#$%^
SELECT @@VERSION; ";

    public const string TALL_DEBUGGING_LINES = @"1
2
3
4
     5   -- 0 based so ... column6 is where '5' starts ... but it's 'slot'/index 5
6
7!
8
/* comment starts on 9,0 
and spans to 10,x */";
    #endregion

    #region Strings (uhhhh) Strings
    public const string SIMPLE_CODESTRING_STRING = @"SELECT 'This is not unicode' [non-unicode]; ";

    public const string SIMPLE_UNICODE_CODESTRING_STRING = @"SELECT N'This is unicode' [unicode]; ";

    public const string GOBS_OF_CODESTRINGS_IN_A_SINGLE_LINE = @"SELECT
    CAST(1 AS bit) [is_exception],
    N'EXCEPTION::> ErrorNumber: ' + CAST(x.exception.value(N'(@error_number)', N'int') AS sysname) + N', LineNumber: ' + CAST(x.exception.value(N'(@error_line)', N'int') AS sysname) + N', Severity: ' + CAST(x.exception.value(N'(@severity)', N'int') AS sysname) + N', Message: ' + x.exception.value(N'.', N'nvarchar(max)') [content]; ";
    #endregion

    #region Comment Strings
    public const string SINGLE_LINE_BLOCK_COMMENT = @"SELECT 'the first line has T-SQL'; 
/* But, line 2 has a comment */     ";

    public const string MINIMAL_MULTI_LINE_COMMENT = @"/* starts at 1,0
and goes until 3,1: (note that * starts at 3,1 ... but the END of the comment is actually at the position of the /... or: 3,2)
*/";

    public const string BASIC_MULTI_LINE_COMMENT = @"SELECT TOP 200 * 
/* This is a simple comment.    
        that spans multiple lines */
FROM dbo.SomeTable; ";

    public const string MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE = @"SELECT 'this is some text' [output] /* and this
is a comment that spans multiple lines. 
But which does NOT start at index 0 on line 1... it starts much later. */ ";

    public const string TALL_MULTI_LINE_COMMENT = @"SELECT 'some basic text here' [output] /* 
this is 
a sample 
comment 
that 
spans
multiple 
lines 
and then 
some
... */
";

    public const string MIXED_BLOCK_COMMENTS = @"SELECT 'start with some normal lines' [blah]; 
SELECT /* Self-contained comment here */ 'this is a mess' [OUTPUT];  /* start 
of a new 
comment */ SELECT 'This is more text' [output]; 
SELECT 'and so is this' [final]; ";

    public const string BLOCK_AND_EOL_COMMENTS = @"SELECT 'this is a setup line' [start] -- and it has a line-end comment
SELECT 'this is line 2' [second line] /* and here is a single-line comment */ 
SELECT 'this is line 3' [third line] /* multi-line comment
that spans down to here */ SELECT 'line 4' [4th line]; -- also a comment";
    #endregion

    #region Build and Include File Strings
    public const string BASIC_BUILD_FILE = @"-- ## ROOT: ..\ ##:: 
-- ## OUTPUT: \\\my_project.sql
-- ## :: This is a build file only (i.e., it stores upgrade/install directives + place-holders for code to drop into admindb, etc.)

/*

    REFERENCE:
        - License, documentation, and source code at: 
        	https://github.com/overachiever-productions/s4/

*/  
DECLARE @somethingVersion sysname;";

    public const string NO_INCLUDE_DIRECTIVES_BUILD_FILE = @"-- ## ROOT: ..\ ##:: 
-- ## OUTPUT: \\\piggly_wiggly.sql
/*
	REFERENCE:
		- License, documentation, and source code at: 
			https://github.com/overachiever-productions/s4/
/* ";

    public const string ULTRA_SIMPLE_INCLUDE_FILE = @"Nested File - Line 1
 Nested File - Line 2
 Nested File - Line 3";
    #endregion

    [Test]
    public void Calibrate_Line_Numbers_And_Column_Positions_Against_Simple_Use_Case()
    {
        // this isn't really a test - it's a sanity check to ensure that CORE logic is working as expected. 
        var fileBody = DEBUGGING_LINES;

        // The '4' is ... on the first line and at position/column-number 4.
        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"4", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(1));
        Assert.That(position.ColumnNumber, Is.EqualTo(3));

        // 1st CRLF is at index 9 (which is on line 1) - which translates to a 'newline' starting at position 11 (the 'C') - or line 2, column 0. 
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"C", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(2));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        // $ is on line 3 - at position/index 3m (2)
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"$", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(3));
        Assert.That(position.ColumnNumber, Is.EqualTo(2));

        // ; is on line 4 - at position 16 (0 based)
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@";", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(4));
        Assert.That(position.ColumnNumber, Is.EqualTo(16));
    }

    [Test]
    public void Calibrate_Line_Numbers_And_Column_Positions_Against_Tall_Use_Case()
    {
        // also not a real unit test - but more of a sanity-check + option for easy debugging of CORE logic.
        var fileBody = TALL_DEBUGGING_LINES; 

        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"4", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(4));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"5", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(5));
        Assert.That(position.ColumnNumber, Is.EqualTo(5));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"!", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(7));
        Assert.That(position.ColumnNumber, Is.EqualTo(1));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"/*", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(9));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"*/", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(10));
        Assert.That(position.ColumnNumber, Is.EqualTo(18));
    }

    [Test]
    public void ProcessLines_Captures_Simple_Non_Unicode_CodeString()
    {
        var fileBody = SIMPLE_CODESTRING_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(1));

        var codeString = result.CodeStrings[0];
        StringAssert.AreEqualIgnoringCase(@"'This is not unicode'", codeString.Text);

        Assert.That(result.Lines.Count, Is.EqualTo(1));
        var line1 = result.Lines[0];
        Assert.That(line1.LineType.HasFlag(LineType.ContainsStrings));
        Assert.False(line1.LineType.HasFlag(LineType.ContainsComments));

        StringAssert.AreEqualIgnoringCase(@"'This is not unicode'", line1.CodeStrings[0].Text);
        Assert.That(line1.CodeStrings[0].ColumnStart, Is.EqualTo(7));
        Assert.That(line1.CodeStrings[0].ColumnEnd, Is.EqualTo(27));
    }

    [Test]
    public void ProcessLines_Captures_Simple_Unicode_CodeString()
    {
        var fileBody = SIMPLE_UNICODE_CODESTRING_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(1));

        var codeString = result.CodeStrings[0];
        StringAssert.AreEqualIgnoringCase(@"N'This is unicode'", codeString.Text);

        Assert.That(result.Lines.Count, Is.EqualTo(1));
        var line1 = result.Lines[0];
        Assert.That(line1.LineType.HasFlag(LineType.ContainsStrings));
        Assert.False(line1.LineType.HasFlag(LineType.ContainsComments));

        StringAssert.AreEqualIgnoringCase(@"N'This is unicode'", line1.CodeStrings[0].Text);
        Assert.That(line1.CodeStrings[0].ColumnStart, Is.EqualTo(7));
        Assert.That(line1.CodeStrings[0].ColumnEnd, Is.EqualTo(24));
    }

    [Test]
    public void ProcessLines_Captures_Multiple_Single_Line_CodeStrings()
    {
        var fileBody = GOBS_OF_CODESTRINGS_IN_A_SINGLE_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(12));

        var codeString = result.CodeStrings[0];
        StringAssert.AreEqualIgnoringCase(@"N'EXCEPTION::> ErrorNumber: '", codeString.Text);
        Assert.That(codeString.LineStart, Is.EqualTo(3));
        Assert.That(codeString.ColumnStart, Is.EqualTo(4));
        Assert.That(codeString.ColumnEnd, Is.EqualTo(32));

        codeString = result.CodeStrings[11];
        StringAssert.AreEqualIgnoringCase(@"N'nvarchar(max)'", codeString.Text);
        Assert.That(codeString.LineStart, Is.EqualTo(3));
        Assert.That(codeString.ColumnStart, Is.EqualTo(304));
        Assert.That(codeString.ColumnEnd, Is.EqualTo(319));
    }

    [Test]
    public void ProcessLines_Captures_Single_Multi_Line_CodeString()
    {
    }

    [Test]
    public void ProcessLines_Captures_Complex_CodeStrings_Against_Multiple_Lines()
    {

    }

    [Test]
    public void Calibrate_Multi_Line_Comment_Start_And_End_Positions()
    {
        // also not quite a test - but more of a sanity check and/or for debugging. 
        var fileBody = MINIMAL_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        Assert.That(comment.LineStart, Is.EqualTo(1));
        Assert.That(comment.ColumnStart, Is.EqualTo(0));

        Assert.That(comment.LineEnd, Is.EqualTo(3));
        Assert.That(comment.ColumnEnd, Is.EqualTo(1));
    }

    [Test]
    public void Calibrate_Start_And_End_Positions_For_Single_Line_Block_Comment()
    {
        var fileBody = SINGLE_LINE_BLOCK_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        Assert.That(comment.LineStart, Is.EqualTo(2));
        Assert.That(comment.ColumnStart, Is.EqualTo(0));

        Assert.That(comment.LineEnd, Is.EqualTo(2));
        Assert.That(comment.ColumnEnd, Is.EqualTo(30));
    }

    [Test]
    public void ProcessLines_Gets_Start_And_End_Positions_Of_Multi_Line_Comment()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];
        
        Assert.That(comment.LineStart, Is.EqualTo(2));
        Assert.That(comment.ColumnStart, Is.EqualTo(0));

        Assert.That(comment.LineEnd, Is.EqualTo(3));
        Assert.That(comment.ColumnEnd, Is.EqualTo(35));

        StringAssert.StartsWith(@"/* This is a simple comment.", comment.Text);
        StringAssert.EndsWith(@"        that spans multiple lines */", comment.Text);
    }

    [Test]
    public void ProcessLines_Captures_Simple_Multi_Line_Comment_Text()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        StringAssert.StartsWith(@"/* This is a simple comment.", comment.Text);
        StringAssert.EndsWith(@"        that spans multiple lines */", comment.Text);
    }

    [Test]
    public void ProcessLines_Assigns_Location_To_Comment()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        Assert.That(comment.Location.Count, Is.EqualTo(1));
        Assert.That(comment.Location.Peek().FileName, Is.EqualTo("build.sql"));
        Assert.That(comment.Location.Peek().LineNumber, Is.EqualTo(2));

        Assert.That(comment.Location.Peek().ColumnNumber, Is.EqualTo(0));
    }

    [Test]
    public void ProcessLines_Assigns_Start_Index_To_Comment_Location()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        Assert.That(comment.Location.Count, Is.EqualTo(1));
        Assert.That(comment.Location.Peek().FileName, Is.EqualTo("build.sql"));
        Assert.That(comment.Location.Peek().LineNumber, Is.EqualTo(1));

        Assert.That(comment.Location.Peek().ColumnNumber, Is.EqualTo(36));
    }

    [Test]
    public void ProcessLines_Captures_Comment_Start_And_End_Lines_And_Indexes()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var comment = result.Comments[0];

        Assert.That(comment.LineStart, Is.EqualTo(1));
        Assert.That(comment.ColumnStart, Is.EqualTo(36));

        Assert.That(comment.LineEnd, Is.EqualTo(3));
        Assert.That(comment.ColumnEnd, Is.EqualTo(72));
    }

    [Test]
    public void ProcessLines_Marks_Start_Of_MultiLine_Comment_As_Block_Comment_Start()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var startOfCommentLine = result.Lines[0];

        StringAssert.AreEqualIgnoringCase(startOfCommentLine.RawContent, @"SELECT 'this is some text' [output] /* and this");

        Assert.True(startOfCommentLine.HasComment);
        Assert.True(startOfCommentLine.HasBlockComment);
        
        Assert.That(startOfCommentLine.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
    }

    [Test]
    public void Multiple_Block_Comments_On_Single_Line_Are_Captured_Correctly()
    {
        var fileBody = @"	        @server = /* sample line with */ N'PARTNER',   /* multiple block comments */";
        var fileLines = new List<string> { fileBody };

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result.Lines.Count, Is.EqualTo(1));

        Line test = result.Lines[0];
        Assert.True(test.HasComment);
        Assert.True(test.HasBlockComment);

        Assert.That(test.CodeComments.Count, Is.EqualTo(2));
        Assert.That(test.CodeComments[0].LineStart, Is.EqualTo(test.CodeComments[0].LineEnd));

        StringAssert.AreEqualIgnoringCase(@"	        @server =  N'PARTNER',   ", test.GetCodeOnlyText());

        StringAssert.AreEqualIgnoringCase(@"/* sample line with */", test.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@"/* multiple block comments */", test.CodeComments[1].Text);

        Assert.That(test.CodeComments[0].LineStart, Is.EqualTo(1));
        Assert.That(test.CodeComments[1].LineStart, Is.EqualTo(1));

        Assert.That(test.CodeComments[0].ColumnStart, Is.EqualTo(19));
        Assert.That(test.CodeComments[1].ColumnStart, Is.EqualTo(56));

        Assert.That(test.CodeComments[0].ColumnEnd, Is.EqualTo(40));
        Assert.That(test.CodeComments[1].ColumnEnd, Is.EqualTo(84));
    }

    [Test]
    public void Single_Line_Block_Comment_Plus_MultiLine_Block_Comment_Start_Can_Live_Together()
    {
        var fileBody = MIXED_BLOCK_COMMENTS;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        // Check RESULT comments first:
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(5));
        Assert.That(result.Comments.Count, Is.EqualTo(2));

        Assert.That(result.Comments[0].LineStart, Is.EqualTo(2));
        Assert.That(result.Comments[0].LineEnd, Is.EqualTo(2));

        Assert.That(result.Comments[1].LineStart, Is.EqualTo(2));
        Assert.That(result.Comments[1].LineEnd, Is.EqualTo(4));

        StringAssert.AreEqualIgnoringCase(@"/* Self-contained comment here */", result.Comments[0].Text);
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", result.Comments[1].Text);

        // Now check each LINE's comments:
        var line0 = result.Lines[0];
        Assert.False(line0.HasComment);

        var startLine = result.Lines[1];
        Assert.That(startLine.LineNumber, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"SELECT /* Self-contained comment here */ 'this is a mess' [OUTPUT];  /* start ", startLine.RawContent);

        Assert.That(startLine.HasComment);
        Assert.That(startLine.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(startLine.CommentType.HasFlag(CommentType.None));
        Assert.False(startLine.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.That(startLine.BlockCommentType.HasFlag(BlockCommentType.SingleLine));
        Assert.That(startLine.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));  // this particular codeLine should have BOTH .SIngleLine AND .MultiLineSTART
        Assert.False(startLine.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));
        Assert.False(startLine.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));

        Assert.That(startLine.CodeComments.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"/* Self-contained comment here */", startLine.CodeComments[0].Text);
        StringAssert.StartsWith(@"/* start", startLine.CodeComments[1].Text);
        StringAssert.AreEqualIgnoringCase(@"SELECT  'this is a mess' [OUTPUT];  ", startLine.GetCodeOnlyText());

        var middleLine = result.Lines[2];
        Assert.That(middleLine.LineNumber, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(@"of a new ", middleLine.RawContent);
        
        Assert.That(middleLine.HasComment);
        Assert.That(middleLine.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(middleLine.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.False(middleLine.CommentType.HasFlag(CommentType.None));
        Assert.That(middleLine.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));
        Assert.False(middleLine.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.False(middleLine.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));

        Assert.That(middleLine.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", middleLine.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@"", middleLine.GetCodeOnlyText());

        var lineEnd = result.Lines[3];
        Assert.That(lineEnd.LineNumber, Is.EqualTo(4));

        Assert.That(lineEnd.HasComment);
        Assert.That(lineEnd.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(lineEnd.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.False(lineEnd.CommentType.HasFlag(CommentType.None));
        Assert.That(lineEnd.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));
        Assert.False(lineEnd.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.False(lineEnd.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));

        Assert.That(lineEnd.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", lineEnd.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@" SELECT 'This is more text' [output]; ", lineEnd.GetCodeOnlyText());

        var finalLine = result.Lines[4];
        Assert.That(finalLine.LineNumber, Is.EqualTo(5));
        Assert.False(finalLine.HasComment);
        
    }

    [Test]
    public void EndOfLine_Comments_And_Block_Comments_Can_Live_Together()
    {
        var fileBody = BLOCK_AND_EOL_COMMENTS;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        // Check RESULT comments first:
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(4));
        Assert.That(result.Comments.Count, Is.EqualTo(4));

        StringAssert.AreEqualIgnoringCase(@"-- and it has a line-end comment", result.Comments[0].Text);

        // TODO: Hmmm... this is 'right'. End-of-Line comments are always added first - then multi-line comments. 
        //          short of interleaving line-processing and file-processing... that's how things are going to be. 
        //          SO. the 'todo' here is to ... document that this is how things behave. 
        //              and... i don't think this'll actually, ever, be a real problem. 
        StringAssert.AreEqualIgnoringCase(@"-- also a comment", result.Comments[1].Text);
        StringAssert.AreEqualIgnoringCase(@"/* and here is a single-line comment */", result.Comments[2].Text);
        StringAssert.AreEqualIgnoringCase("/* multi-line comment\r\nthat spans down to here */", result.Comments[3].Text);

        // Now check comments per each line: 
        var line1 = result.Lines[0];
        Assert.That(line1.HasComment);
        Assert.That(line1.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.False(line1.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(line1.CommentType.HasFlag(CommentType.None));
        Assert.False(line1.LineEndCommentType.HasFlag(LineEndCommentType.FullLineComment));
        Assert.False(line1.LineEndCommentType.HasFlag(LineEndCommentType.WhiteSpaceAndComment));
        Assert.That(line1.LineEndCommentType.HasFlag(LineEndCommentType.EolComment));

        Assert.That(line1.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is a setup line' [start] -- and it has a line-end comment", line1.RawContent);
        StringAssert.AreEqualIgnoringCase(@"-- and it has a line-end comment", line1.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@"-- and it has a line-end comment", line1.GetCommentText());
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is a setup line' [start] ", line1.GetCodeOnlyText());

        var line2 = result.Lines[1];
        Assert.That(line2.HasComment);
        Assert.False(line2.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.That(line2.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(line2.CommentType.HasFlag(CommentType.None));
        Assert.That(line2.BlockCommentType.HasFlag(BlockCommentType.SingleLine));
        Assert.False(line2.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.False(line2.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));
        Assert.False(line2.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));

        Assert.That(line2.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 2' [second line] /* and here is a single-line comment */ ", line2.RawContent);
        StringAssert.AreEqualIgnoringCase(@"/* and here is a single-line comment */", line2.GetCommentText());
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 2' [second line]  ", line2.GetCodeOnlyText());

        var line3 = result.Lines[2];
        Assert.That(line3.HasComment);
        Assert.False(line3.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.That(line3.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(line3.CommentType.HasFlag(CommentType.None));
        Assert.False(line3.BlockCommentType.HasFlag(BlockCommentType.SingleLine));
        Assert.That(line3.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.False(line3.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));
        Assert.False(line3.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));

        Assert.That(line3.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 3' [third line] /* multi-line comment", line3.RawContent);
        StringAssert.AreEqualIgnoringCase("/* multi-line comment\r\nthat spans down to here */", line3.GetCommentText());
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 3' [third line] ", line3.GetCodeOnlyText());

        var line4 = result.Lines[3];
        Assert.That(line4.HasComment);
        Assert.That(line4.CommentType.HasFlag(CommentType.LineEndComment));  // both a block-end and a 'line-end' comment are present.
        Assert.That(line4.CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(line4.CommentType.HasFlag(CommentType.None));
        Assert.False(line4.BlockCommentType.HasFlag(BlockCommentType.SingleLine));
        Assert.False(line4.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.That(line4.BlockCommentType.HasFlag(BlockCommentType.MultilineEnd));
        Assert.False(line4.BlockCommentType.HasFlag(BlockCommentType.MultilineLine));

        Assert.That(line4.CodeComments.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"that spans down to here */ SELECT 'line 4' [4th line]; -- also a comment", line4.RawContent);

        // TODO: this is also showing similar 'problems' to the above... 
        //      only, again, not sure it really matters. As I don't think I can use .GetCommentText() for anything truly critical. 
        StringAssert.AreEqualIgnoringCase("-- also a comment/* multi-line comment\r\nthat spans down to here */", line4.GetCommentText());
        StringAssert.AreEqualIgnoringCase(@" SELECT 'line 4' [4th line]; ", line4.GetCodeOnlyText());
    }

    [Test]
    public void Basic_Build_File_Yields_Correct_Number_of_Code_Lines()
    {
        var buildFile = BASIC_BUILD_FILE;
        var fileLines = Regex.Split(buildFile, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(buildFile);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var sut = new BuildFile("build.sql", fileManager.Object);

        Assert.That(sut.Lines.Count, Is.EqualTo(12));
    }

    [Test]
    public void GetFileLineNumber_Gets_Correct_Positions_Against_Simple_Use_Cases()
    {
        var fileBody = BASIC_BUILD_FILE;

        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"/*", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(5));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"REFERENCE:", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(7));
        Assert.That(position.ColumnNumber, Is.EqualTo(4));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"github.com", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(9));
        Assert.That(position.ColumnNumber, Is.EqualTo(17));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"*/", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(11));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"DECLARE @som", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(12));
        Assert.That(position.ColumnNumber, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"OUTPUT:", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(2));
        Assert.That(position.ColumnNumber, Is.EqualTo(6));
    }

    [Test]
    public void Build_File_With_No_Includes_Correctly_Identifies_Source_File_Locations()
    {
        var buildFile = NO_INCLUDE_DIRECTIVES_BUILD_FILE;
        var fileLines = Regex.Split(buildFile, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(buildFile);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var sut = new BuildFile("simple.build.sql", fileManager.Object);

        Assert.That(sut.Lines.Count, Is.EqualTo(7));

        Assert.That(sut.Lines[0].Location.Count, Is.EqualTo(1));
        Assert.That(sut.Lines[0].Location.Peek().FileName, Is.EqualTo("simple.build.sql"));
        Assert.That(sut.Lines[0].Location.Peek().LineNumber, Is.EqualTo(1));

        Assert.That(sut.Lines[6].Location.Count, Is.EqualTo(1));
        Assert.That(sut.Lines[6].Location.Peek().FileName, Is.EqualTo("simple.build.sql"));
        Assert.That(sut.Lines[6].Location.Peek().LineNumber, Is.EqualTo(7));
    }

    [Test]
    public void Build_File_With_Simple_Include_Identifies_Source_For_Both_Files()
    {
        var line4 = new Line("simple.build.sql", 4, @"-- ## FILE: SomePath.sql ");
        var includeFile = ULTRA_SIMPLE_INCLUDE_FILE;
        var fileLines = Regex.Split(includeFile, @"\r\n|\r|\n", Global.SingleLineOptions).ToList();

        var fileManager = new Mock<IFileManager>();

        fileManager.Setup(x => x.GetFileContent("SomePath.sql"))
            .Returns(includeFile);
        fileManager.Setup(x => x.GetFileLines("SomePath.sql"))
            .Returns(fileLines);

        // Simulate processing of line #4 (i.e., assume that BuildFile processing was fine/as-expected.
        var sut = FileProcessor.ProcessFileLines(line4, "SomePath.sql", ProcessingType.IncludedFile, fileManager.Object, "N/A", "N/A");

        Assert.That(sut.Lines.Count, Is.EqualTo(3));

        Assert.That(sut.Lines[0].Location.Count, Is.EqualTo(2));
        Assert.That(sut.Lines[1].Location.Count, Is.EqualTo(2));
        Assert.That(sut.Lines[2].Location.Count, Is.EqualTo(2));

        var location = sut.Lines[1].Location;

        Assert.That(location.Pop().FileName, Is.EqualTo("SomePath.sql"));
        Assert.That(location.Pop().FileName, Is.EqualTo("simple.build.sql"));
    }
}