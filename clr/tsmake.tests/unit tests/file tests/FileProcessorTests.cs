using tsmake;
using tsmake.models;

namespace tsmake.tests.unit_tests.file_tests;

[TestFixture]
public class FileProcessorTests
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

    public const string SIMPLE_EOL_COMMENT_AS_STRING_CONTENT = @"DECLARE @text nvarchar(MAX) = N'-- this is a comment (in a string)  ';";

    public const string COMMENTS_AS_STRING_CONTENT = @"SELECT N'This is just a normal string - no comments' [OUTPUT1];
DECLARE @sql nvarchar(MAX) = N'/* ----------------------------------------------
-- This looks like a ''line-end'' comment, but it's actually part of a block-comment. 
-------------------------------------*/ 
-- this is a real line-end comment. 
/* this is a real 
block comment */";
    #endregion

    #region Strings
    public const string SIMPLE_CODESTRING_STRING = @"SELECT 'This is not unicode' [non-unicode]; ";

    public const string SIMPLE_UNICODE_CODESTRING_STRING = @"SELECT N'This is unicode' [unicode]; ";

    public const string GOBS_OF_CODESTRINGS_IN_A_SINGLE_LINE = @"SELECT
    CAST(1 AS bit) [is_exception],
    N'EXCEPTION::> ErrorNumber: ' + CAST(x.exception.value(N'(@error_number)', N'int') AS sysname) + N', LineNumber: ' + CAST(x.exception.value(N'(@error_line)', N'int') AS sysname) + N', Severity: ' + CAST(x.exception.value(N'(@severity)', N'int') AS sysname) + N', Message: ' + x.exception.value(N'.', N'nvarchar(max)') [content]; ";

    public const string MULTIPLE_MULTILINE_STRINGS = @"SELECT N'PRINT N''This is a 
multi 
multiline 
 string''' [thing_one], N'PRINT N''And so 
is this''' [thing_two];";

    public const string SINGLE_MULTILINE_STRING = @"SELECT 24 [non-string];
SELECT N'This string wraps 
down 
to 
multiple
lines' [mult_line_string];";
    #endregion

    #region Comments
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

    public const string HEADER_COMMENTS_NESTED_IN_STRING = @"-- 13
DECLARE @complex nvarchar(MAX) = N'/*----------------------------'
+ N'-- header'
+ N'-------------------------------------------- */'
-- 17";
    #endregion

    [Test]
    public void Calibrate_Line_Numbers_And_Column_Positions_Against_Simple_Use_Case()
    {
        // this isn't really a test - it's a sanity check to ensure that CORE logic is working as expected. 
        var fileBody = DEBUGGING_LINES;

        // The '4' is ... on the first line and at position/column-number 4.
        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"4", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(1));
        Assert.That(position.Column, Is.EqualTo(3));

        // 1st CRLF is at index 9 (which is on line 1) - which translates to a 'newline' starting at position 11 (the 'C') - or line 2, column 0. 
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"C", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(2));
        Assert.That(position.Column, Is.EqualTo(0));

        // $ is on line 3 - at position/index 3m (2)
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"$", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(3));
        Assert.That(position.Column, Is.EqualTo(2));

        // ; is on line 4 - at position 16 (0 based)
        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@";", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(4));
        Assert.That(position.Column, Is.EqualTo(16));
    }

    [Test]
    public void Calibrate_Line_Numbers_And_Column_Positions_Against_Tall_Use_Case()
    {
        // also not a real unit test - but more of a sanity-check + option for easy debugging of CORE logic.
        var fileBody = TALL_DEBUGGING_LINES; 

        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"4", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(4));
        Assert.That(position.Column, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"5", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(5));
        Assert.That(position.Column, Is.EqualTo(5));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"!", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(7));
        Assert.That(position.Column, Is.EqualTo(1));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"/*", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(9));
        Assert.That(position.Column, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"*/", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(10));
        Assert.That(position.Column, Is.EqualTo(18));
    }

    [Test]
    public void Calibrate_Simple_Comment_is_Marked_As_Line_End_Comment()
    {
        var fileBody = @"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' -- this is a simple comment ";
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var line1 = result.Lines[0];

        Assert.That(line1.HasComment);
        Assert.That(line1.CodeComments[0].CommentType.HasFlag(CommentType.LineEndComment));
        StringAssert.AreEqualIgnoringCase(@"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' ", line1.GetCodeOnlyText());
        StringAssert.AreEqualIgnoringCase(@"-- this is a simple comment ", line1.CodeComments[0].CurrentLineText);
    }

    [Test]
    public void Calibrate_Multi_Line_Comment_Start_And_End_Positions()
    {
        // also not quite a test - but more of a sanity check and/or for debugging. 
        var fileBody = MINIMAL_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
    public void Calibrate_LineEnd_Comment_Inside_String_Is_Ignored()
    {
        var fileBody = SIMPLE_EOL_COMMENT_AS_STRING_CONTENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(1));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(1));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
    }

    [Test]
    public void Calibrate_MultiLine_Comments_Nested_As_MultiLine_String_Data()
    {
        var fileBody = COMMENTS_AS_STRING_CONTENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(7));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(2));
        Assert.That(result.Comments.Count, Is.EqualTo(2));

        var line1 = result.Lines[0];
        StringAssert.AreEqualIgnoringCase(@"SELECT N'This is just a normal string - no comments' [OUTPUT1];", line1.RawContent);
        Assert.False(line1.HasBlockComment);
        Assert.That(line1.HasString);

        var line2 = result.Lines[1];
        StringAssert.AreEqualIgnoringCase(@"DECLARE @sql nvarchar(MAX) = N'/* ----------------------------------------------", line2.RawContent);
        //Assert.That(line2.HasString);
        Assert.False(line2.HasComment);

        var line3 = result.Lines[2];
        
        var line4 = result.Lines[3];
        
        var line5 = result.Lines[4];
        
        var line6 = result.Lines[5];
        
        var line7 = result.Lines[6];

    }

    [Test]
    public void ProcessLines_Correctly_Identifies_WhiteSpace_And_LineEnd_Comment()
    {
        var fileBody = @"  -- This is a comment - but there was whitespace in front of it.";
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var line1 = result.Lines[0];

        Assert.That(line1.HasComment);
        Assert.That(line1.CodeComments[0].CommentType.HasFlag(CommentType.LineEndComment));
    }

    [Test]
    public void Process_Lines_Correctly_Identifies_LineEnd_Comment_With_No_Code_Text()
    {
        var fileBody = @"-----------------------------------";
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        var line1 = result.Lines[0];

        Assert.That(line1.HasComment);
        Assert.That(line1.CodeComments[0].CommentType.HasFlag(CommentType.LineEndComment));
    }

    [Test]
    public void ProcessLines_Captures_Simple_CodeString()
    {
        var fileBody = SIMPLE_CODESTRING_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        Assert.That(codeString.Location, Is.Not.Null);
        Assert.That(codeString.Location.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"build.sql", codeString.Location.Peek().FileName);
        Assert.That(codeString.Location.Peek().LineNumber, Is.EqualTo(1));
        Assert.That(codeString.Location.Peek().Column, Is.EqualTo(7));
        StringAssert.AreEqualIgnoringCase(@"build.sql (1, 7)", codeString.GetLocation());

        Assert.That(result.Lines.Count, Is.EqualTo(1));

        var line1 = result.Lines[0];
        Assert.That(line1.HasString);
        Assert.False(line1.HasComment);

        StringAssert.AreEqualIgnoringCase(@"'This is not unicode'", line1.CodeStrings[0].Text);
        Assert.That(line1.CodeStrings[0].ColumnStart, Is.EqualTo(7));
        Assert.That(line1.CodeStrings[0].ColumnEnd, Is.EqualTo(27));
    }

    [Test]
    public void ProcessLines_Captures_Simple_Unicode_CodeString()
    {
        var fileBody = SIMPLE_UNICODE_CODESTRING_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        Assert.That(line1.HasString);
        Assert.False(line1.HasComment);

        StringAssert.AreEqualIgnoringCase(@"N'This is unicode'", line1.CodeStrings[0].Text);
        Assert.That(line1.CodeStrings[0].ColumnStart, Is.EqualTo(7));
        Assert.That(line1.CodeStrings[0].ColumnEnd, Is.EqualTo(24));
    }

    [Test]
    public void ProcessLines_Correctly_Ignores_MultiLine_Comments_Within_Strings()
    {
        var fileBody = HEADER_COMMENTS_NESTED_IN_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(5));
        Assert.That(result.Comments.Count, Is.EqualTo(2));  // don't forget that -- 13 and -- 17 are ... comments. 
        Assert.That(result.CodeStrings.Count, Is.EqualTo(3));  // NOTE that @complex is ... 3x distinct strings... 

        var line1 = result.Lines[0];
        Assert.That(line1.LineNumber, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"-- 13", line1.RawContent);
        Assert.That(line1.HasComment);
        Assert.That(line1.HasLineEndComment);
        Assert.False(line1.HasBlockComment);
        StringAssert.AreEqualIgnoringCase(@"-- 13", line1.CodeComments[0].Text);
        Assert.False(line1.HasString);
        Assert.False(line1.HasTokens);
        Assert.False(line1.IsDirective);

        var line2 = result.Lines[1];
        Assert.That(line2.LineNumber, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"DECLARE @complex nvarchar(MAX) = N'/*----------------------------'", line2.RawContent);
        StringAssert.AreEqualIgnoringCase(@"N'/*----------------------------'", line2.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase(@"N'/*----------------------------'", line2.CodeStrings[0].CurrentLineText);
        // TODO: see the TODO/WARNING down below for line #4. 
        Assert.False(line2.HasComment); // this is obviously the 'biggie'. 
        Assert.That(line2.HasString);
        Assert.False(line2.HasTokens);
        Assert.False(line2.IsDirective);

        var line3 = result.Lines[2];
        Assert.That(line3.LineNumber, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(@"+ N'-- header'", line3.RawContent);
        StringAssert.AreEqualIgnoringCase(@"N'-- header'", line3.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase(@"N'-- header'", line3.CodeStrings[0].CurrentLineText);
        Assert.False(line3.HasComment);
        Assert.That(line3.HasString);
        Assert.False(line3.HasTokens);
        Assert.False(line3.IsDirective);

        var line4 = result.Lines[3];
        Assert.That(line4.LineNumber, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase(@"+ N'-------------------------------------------- */'", line4.RawContent);
        StringAssert.AreEqualIgnoringCase(@"N'-------------------------------------------- */'", line4.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase(@"N'-------------------------------------------- */'", line4.CodeStrings[0].CurrentLineText);

        // TODO: 
        // WARNING: 
        // I THINK that the following (code) line is TRUE only because there was a MATCH of "/*----------------------------" as being part of (i.e., .contains) against "'/*----------------------------'"
        //      which is proper/correct. At that point, the ENTIRE block-comment (spanning 3x lines) is marked for removal from each of the lines it impacts... 
        //          AND, that's what's totally happening - which is GREAT. 
        //  THE PROBLEM, is that on line #3, line3.CodeStrings[0].CurrentTextLine = "N'-- header'"  (which is 100% correct). 
        //      BUT, line3.CodeComment[0].CurrentTextLine = "+ N'-- header'"  (note the "+ Nxxxx") - which is what it SORT OF should be (the entire /* comment - from lines 2 - 4 */ gets 'split' into 3x pieces - one per line. 
        //          with the point being: "+ N'-- header'" will NEVER be 'contained' within the STRING body itself (which is just "N'-- header") because the STRING has a 
        //          narrower SCOPE than the 'line' of comment text. 
        //      So, this either just ends up working 'accidentally' and is always (which'd be crazy) going to be fine... 
        //      OR, I'm going to have to look at ways to ... well, ignore ENTIRE /* block comments */ if/when they START in a string. 
        //              and, arguably, the logic I've just described does JUST this (i.e., it finds that the START of a /* block comment */ is in a string... 
        //              and, as such - removes the ENTIRETY of the block comment out of the string. 
        //          I'm worried, though, that this is just a 'happy accident'. 
        //          in fact. what happens when: 
        //              a) there's MORE in the 'build file' than just 'string'[CR][LF]+ 'string]+[CR][LF]+'string' ? i.e., i've got a VERY nice, tidy, string for HEADER_COMMENTS_NESTED_IN_STRING
        //                      what happens if there are IF/ELSE statements intermingled in there?  or ... other things. or @complex = '/*start here'; ... more stuff and then... @complex += ' end of stuff */
        //                          yeah. think i'm effed. 
        //              b) what if I had: 1) /* real comment (not in a string) start here.... 
        //                                 2) some more comments here ... 
        //                                  3) somehow ... something like 'hmmm this is a string */' 
        //                          such that I managed to... somehow 'terminate' the start of a real/normal /* block */ down... inside of a 'string'? 

        Assert.False(line4.HasComment);
        Assert.That(line4.HasString);
        Assert.False(line4.HasTokens);
        Assert.False(line4.IsDirective);

        var line5 = result.Lines[4];
        Assert.That(line5.LineNumber, Is.EqualTo(5));
        StringAssert.AreEqualIgnoringCase(@"-- 17", line5.RawContent);
        Assert.That(line5.HasComment);
        Assert.False(line5.HasString);
        Assert.False(line5.HasTokens);
        Assert.False(line5.IsDirective);
    }

    [Test]
    public void ProcessLines_Captures_Multiple_Single_Line_CodeStrings()
    {
        var fileBody = GOBS_OF_CODESTRINGS_IN_A_SINGLE_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        var fileBody = SINGLE_MULTILINE_STRING;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(6));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(1));

        var codeString = result.CodeStrings[0];
        StringAssert.AreEqualIgnoringCase("N'This string wraps \r\ndown \r\nto \r\nmultiple\r\nlines'", codeString.Text);
        Assert.That(codeString.LineStart, Is.EqualTo(2));
        Assert.That(codeString.ColumnStart, Is.EqualTo(7));
        Assert.That(codeString.LineEnd, Is.EqualTo(6));
        Assert.That(codeString.ColumnEnd, Is.EqualTo(5));

        var line1 = result.Lines[0];
        Assert.That(line1.CodeStrings.Count, Is.EqualTo(0));
        Assert.That(line1.CodeComments.Count, Is.EqualTo(0));

        var line2 = result.Lines[1];
        Assert.That(line2.CodeStrings.Count, Is.EqualTo(1));
        Assert.That(line2.CodeComments.Count, Is.EqualTo(0));
        StringAssert.AreEqualIgnoringCase("N'This string wraps \r\ndown \r\nto \r\nmultiple\r\nlines'", line2.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("N'This string wraps ", line2.CodeStrings[0].CurrentLineText);

        var line3 = result.Lines[2];
        Assert.That(line3.CodeStrings.Count, Is.EqualTo(1));
        Assert.That(line3.CodeComments.Count, Is.EqualTo(0));
        StringAssert.AreEqualIgnoringCase("N'This string wraps \r\ndown \r\nto \r\nmultiple\r\nlines'", line3.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("down ", line3.CodeStrings[0].CurrentLineText);

        var line4 = result.Lines[3];
        Assert.That(line4.CodeStrings.Count, Is.EqualTo(1));
        Assert.That(line4.CodeComments.Count, Is.EqualTo(0));
        StringAssert.AreEqualIgnoringCase("N'This string wraps \r\ndown \r\nto \r\nmultiple\r\nlines'", line4.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("to ", line4.CodeStrings[0].CurrentLineText);

        var line6 = result.Lines[5];
    }

    [Test]
    public void ProcessLines_Gets_Start_And_End_Positions_Of_Multi_Line_Comment()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
    public void ProcessLines_Captures_Multiple_Multi_Line_Strings()
    {
        var fileBody = MULTIPLE_MULTILINE_STRINGS;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Comments.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(5));
        Assert.That(result.CodeStrings.Count, Is.EqualTo(2));

        var codeString = result.CodeStrings[0];
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a \r\nmulti \r\nmultiline \r\n string'''", codeString.Text);
        Assert.That(codeString.LineStart, Is.EqualTo(1));
        Assert.That(codeString.ColumnStart, Is.EqualTo(7));
        Assert.That(codeString.LineEnd, Is.EqualTo(4));
        Assert.That(codeString.ColumnEnd, Is.EqualTo(9));

        var codeString2 = result.CodeStrings[1];
        StringAssert.AreEqualIgnoringCase("N'PRINT N''And so \r\nis this'''", codeString2.Text);
        Assert.That(codeString2.LineStart, Is.EqualTo(4));
        Assert.That(codeString2.ColumnStart, Is.EqualTo(24));
        Assert.That(codeString2.LineEnd, Is.EqualTo(5));

        var line1 = result.Lines[0];
        Assert.That(line1.CodeStrings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a \r\nmulti \r\nmultiline \r\n string'''", line1.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a ", line1.CodeStrings[0].CurrentLineText);

        var line2 = result.Lines[1];
        Assert.That(line2.CodeStrings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a \r\nmulti \r\nmultiline \r\n string'''", line2.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("multi ", line2.CodeStrings[0].CurrentLineText);

        var line3 = result.Lines[2];
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a \r\nmulti \r\nmultiline \r\n string'''", line3.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("multiline ", line3.CodeStrings[0].CurrentLineText);

        var line4 = result.Lines[3];
        Assert.That(line4.CodeStrings.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase("N'PRINT N''This is a \r\nmulti \r\nmultiline \r\n string'''", line4.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase(" string'''", line4.CodeStrings[0].CurrentLineText);

        StringAssert.AreEqualIgnoringCase("N'PRINT N''And so \r\nis this'''", line4.CodeStrings[1].Text);
        StringAssert.AreEqualIgnoringCase("N'PRINT N''And so ", line4.CodeStrings[1].CurrentLineText);

        var line5 = result.Lines[4];
        Assert.That(line5.CodeStrings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("N'PRINT N''And so \r\nis this'''", line5.CodeStrings[0].Text);
        StringAssert.AreEqualIgnoringCase("is this'''", line5.CodeStrings[0].CurrentLineText);
    }

    [Test]
    public void ProcessLines_Assigns_Location_To_Comment()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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

        Assert.That(comment.Location.Peek().Column, Is.EqualTo(0));
    }

    [Test]
    public void ProcessLines_Captures_Simple_Multi_Line_Comment_Text()
    {
        var fileBody = BASIC_MULTI_LINE_COMMENT;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
    public void ProcessLines_Assigns_Start_Index_To_Comment_Location()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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

        Assert.That(comment.Location.Peek().Column, Is.EqualTo(36));
    }

    [Test]
    public void ProcessLines_Captures_Comment_Start_And_End_Lines_And_Indexes()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
    public void ProcessLines_Marks_Start_Of_MultiLine_Comment_As_Block_Comment()
    {
        var fileBody = MULTI_LINE_BLOCK_COMMENT_STARTING_MID_LINE;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
    }

    [Test]
    public void ProcessLines_Bubbles_Simple_Token_Up_To_Caller()
    {
        var fileBody = "--1\r\n\r\nDECLARE @currentVersion sysname = N'{{##VERSION}}';";
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(fileBody);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var result = FileProcessor.ProcessFileLines(null, "build.sql", ProcessingType.BuildFile, fileManager.Object, "NA", "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Lines.Count, Is.EqualTo(3));
        Assert.That(result.Comments.Count, Is.EqualTo(1));

        Assert.That(result.Tokens.Count, Is.EqualTo(1));

        var token1 = result.Tokens[0];
        Assert.That(token1.Location.LineNumber, Is.EqualTo(3));
        Assert.That(token1.Location.Column, Is.EqualTo(36));
        StringAssert.AreEqualIgnoringCase(@"VERSION", token1.Name);
        Assert.Null(token1.DefaultValue);
    }

    // TODO: 
    // ProcessLines_Bubles_Complex_Tokens... 
    // and 
    // ProcessLines_Bubbles_Tokens_From_Included_Files... 

    [Test]
    // REFACTOR: this name sucks... 
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
        Assert.False(test.HasLineEndComment);

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
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        Assert.That(startLine.HasBlockComment);
        Assert.False(startLine.HasLineEndComment);

        Assert.That(startLine.CodeComments.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"/* Self-contained comment here */", startLine.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", startLine.CodeComments[1].Text);
        StringAssert.AreEqualIgnoringCase(@"/* start ", startLine.CodeComments[1].CurrentLineText);
        StringAssert.AreEqualIgnoringCase(@"SELECT  'this is a mess' [OUTPUT];  ", startLine.GetCodeOnlyText());

        var middleLine = result.Lines[2];
        Assert.That(middleLine.LineNumber, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(@"of a new ", middleLine.RawContent);
        
        Assert.That(middleLine.HasComment);
        Assert.That(middleLine.CodeComments[0].CommentType.HasFlag(CommentType.BlockComment));
        Assert.False(middleLine.CodeComments[0].CommentType.HasFlag(CommentType.LineEndComment));

        Assert.That(middleLine.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", middleLine.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase("of a new ", middleLine.CodeComments[0].CurrentLineText);
        StringAssert.AreEqualIgnoringCase(@"", middleLine.GetCodeOnlyText());

        var lineEnd = result.Lines[3];
        Assert.That(lineEnd.LineNumber, Is.EqualTo(4));

        Assert.That(lineEnd.HasComment);
        Assert.That(lineEnd.HasBlockComment);
        Assert.False(lineEnd.HasLineEndComment);

        Assert.That(lineEnd.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* start \r\nof a new \r\ncomment */", lineEnd.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase("comment */", lineEnd.CodeComments[0].CurrentLineText);
        StringAssert.AreEqualIgnoringCase(@" SELECT 'This is more text' [output]; ", lineEnd.GetCodeOnlyText());

        var finalLine = result.Lines[4];
        Assert.That(finalLine.LineNumber, Is.EqualTo(5));
        Assert.False(finalLine.HasComment);
        Assert.That(finalLine.HasString);
    }

    [Test]
    public void EndOfLine_Comments_And_Block_Comments_Can_Live_Together()
    {
        var fileBody = BLOCK_AND_EOL_COMMENTS;
        var fileLines = Regex.Split(fileBody, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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

        // TODO: Need to document that /* block comments are grabbed/processed first */ and that -- line end comments are done AFTER that. 
        //          And, I don't THINK this'll be anything that ANYONE but me cares about - i.e., this should ONLY be a 'nuance' of the code/logic. 
        StringAssert.AreEqualIgnoringCase(@"/* and here is a single-line comment */", result.Comments[0].Text);
        StringAssert.AreEqualIgnoringCase("/* multi-line comment\r\nthat spans down to here */", result.Comments[1].Text);
        StringAssert.AreEqualIgnoringCase(@"-- and it has a line-end comment", result.Comments[2].Text);
        StringAssert.AreEqualIgnoringCase(@"-- also a comment", result.Comments[3].Text);

        // Now check comments per each line: 
        var line1 = result.Lines[0];
        Assert.That(line1.HasComment);
        Assert.That(line1.HasLineEndComment);
        Assert.False(line1.HasBlockComment);

        Assert.That(line1.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is a setup line' [start] -- and it has a line-end comment", line1.RawContent);
        StringAssert.AreEqualIgnoringCase(@"-- and it has a line-end comment", line1.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is a setup line' [start] ", line1.GetCodeOnlyText());

        var line2 = result.Lines[1];
        Assert.That(line2.LineNumber, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 2' [second line] /* and here is a single-line comment */ ", line2.RawContent);
        Assert.That(line2.HasComment);
        Assert.That(line2.HasBlockComment);
        Assert.False(line2.HasLineEndComment);

        Assert.That(line2.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 2' [second line] /* and here is a single-line comment */ ", line2.RawContent);
        StringAssert.AreEqualIgnoringCase(@"/* and here is a single-line comment */", line2.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 2' [second line]  ", line2.GetCodeOnlyText());

        var line3 = result.Lines[2];
        Assert.That(line3.HasComment);
        Assert.False(line3.HasLineEndComment);
        Assert.That(line3.HasBlockComment);

        Assert.That(line3.CodeComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 3' [third line] /* multi-line comment", line3.RawContent);
        StringAssert.AreEqualIgnoringCase("/* multi-line comment\r\nthat spans down to here */", line3.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase("/* multi-line comment", line3.CodeComments[0].CurrentLineText);
        StringAssert.AreEqualIgnoringCase(@"SELECT 'this is line 3' [third line] ", line3.GetCodeOnlyText());

        var line4 = result.Lines[3];
        Assert.That(line4.HasComment);
        Assert.That(line4.HasLineEndComment);  // both a block-end and a 'line-end' comment are present.
        Assert.That(line4.HasBlockComment);
        Assert.That(line4.CodeComments.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase(@"that spans down to here */ SELECT 'line 4' [4th line]; -- also a comment", line4.RawContent);

        StringAssert.AreEqualIgnoringCase("/* multi-line comment\r\nthat spans down to here */", line4.CodeComments[0].Text);
        StringAssert.AreEqualIgnoringCase("-- also a comment", line4.CodeComments[1].Text);
        StringAssert.AreEqualIgnoringCase(@" SELECT 'line 4' [4th line]; ", line4.GetCodeOnlyText());
    }

    [Test]
    public void GetFileLineNumber_Gets_Correct_Positions_Against_Simple_Use_Cases()
    {
        var fileBody = BASIC_BUILD_FILE;

        var position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"/*", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(5));
        Assert.That(position.Column, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"REFERENCE:", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(7));
        Assert.That(position.Column, Is.EqualTo(4));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"github.com", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(9));
        Assert.That(position.Column, Is.EqualTo(17));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"*/", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(11));
        Assert.That(position.Column, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"DECLARE @som", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(12));
        Assert.That(position.Column, Is.EqualTo(0));

        position = FileProcessor.GetFilePositionByCharacterIndex(fileBody, fileBody.IndexOf(@"OUTPUT:", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(position.LineNumber, Is.EqualTo(2));
        Assert.That(position.Column, Is.EqualTo(6));
    }

    [Test]
    public void Basic_Build_File_Yields_Correct_Number_of_Code_Lines()
    {
        var buildFile = BASIC_BUILD_FILE;
        var fileLines = Regex.Split(buildFile, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(x => x.GetFileContent(It.IsAny<string>()))
            .Returns(buildFile);

        fileManager.Setup(x => x.GetFileLines(It.IsAny<string>()))
            .Returns(fileLines);

        var sut = new BuildFile("build.sql", fileManager.Object);

        Assert.That(sut.Lines.Count, Is.EqualTo(12));
    }

    [Test]
    public void Build_File_With_No_Includes_Correctly_Identifies_Source_File_Locations()
    {
        var buildFile = NO_INCLUDE_DIRECTIVES_BUILD_FILE;
        var fileLines = Regex.Split(buildFile, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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
        var fileLines = Regex.Split(includeFile, @"\r\n|\r|\n", Global.StandardRegexOptions).ToList();

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