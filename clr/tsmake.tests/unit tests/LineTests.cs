using tsmake;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class LineTests
{
    [Test]
    public void WhiteSpaceLine_Is_Both_Raw_And_WhiteSpace()
    {
        var sut = new Line("some file.sql", 11, "");

        Assert.That(sut.LineType.HasFlag(LineType.RawContent));
        Assert.That(sut.LineType.HasFlag(LineType.WhiteSpaceOnly));
    }

    [Test]
    public void Directive_Is_Directive()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line("some file.sql", 11, directiveLine);

        Assert.That(sut.LineType.HasFlag(LineType.Directive));
    }

    [Test]
    public void Directive_Is_Not_WhiteSpace()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line("some file.sql", 11, directiveLine);

        // wth?
        Assert.False(sut.LineType.HasFlag(LineType.WhiteSpaceOnly));
    }

    [Test]
    public void Directive_Is_Not_RawContent()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line("some file.sql", 11, directiveLine);

        Assert.False(sut.LineType.HasFlag(LineType.RawContent));
    }

    [Test]
    public void Empty_Line_Is_Not_Marked_As_Comment()
    {
        var sut = new Line("build.sql", 18, "");

        Assert.False(sut.LineType.HasFlag(LineType.ContainsComments));
    }

    [Test]
    public void Non_Comment_Is_Not_Marked_As_Comment()
    {
        string codeLine = @"SELECT @objectId = [object_id], @createDate = create_date FROM master.sys.objects WHERE [name] = N'dba_DatabaseBackups_Log';";

        var sut = new Line("build.sql", 95, codeLine);

        Assert.False(sut.LineType.HasFlag(LineType.ContainsComments));
    }

    [Test]
    public void Simple_Comment_Is_Marked_As_LineEnd_Comment()
    {
        var codeLine = @"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' -- this is a simple comment ";

        var sut = new Line("build.sql", 66, codeLine);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.LineEndComment));
    }

    [Test]
    public void Simple_Comment_That_Is_WhiteSpace_And_Comment_Only_Is_Correctly_Marked()
    {
        var codeLine = @"  -- This is a comment - but there was whitespace in front of it.";

        var sut = new Line("build.sql", 876, codeLine);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.That(sut.LineEndCommentType.HasFlag(LineEndCommentType.WhiteSpaceAndComment));
    }

    [Test]
    public void Simple_Comment_With_No_WhiteSpace_Is_Marked_As_FullLineComment()
    {
        var dashedLine = @"-----------------------------------";

        var sut = new Line("build.sql", 112, dashedLine);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.LineEndComment));
        Assert.That(sut.LineEndCommentType.HasFlag(LineEndCommentType.FullLineComment));
    }

    [Test]
    public void Simple_Comment_Captures_Code_Text_Before_Comment()
    {
        var codeLine = @"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' -- this is a simple comment ";

        var sut = new Line("build.sql", 66, codeLine);

        StringAssert.AreEqualIgnoringCase(@"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' ", sut.GetCodeOnlyText());
    }

    [Test]
    public void Simple_Comment_Captures_Comment_Text_And_Position_Details()
    {
        var codeLine = @"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' -- this is a simple comment ";

        var sut = new Line("build.sql", 66, codeLine);

        StringAssert.AreEqualIgnoringCase(@"-- this is a simple comment ", sut.GetCommentText());
        Assert.That(sut.CodeComments.Count, Is.EqualTo(1));

        Assert.That(sut.CodeComments[0].LineStart, Is.EqualTo(66));
        Assert.That(sut.CodeComments[0].ColumnStart, Is.EqualTo(62));

        Assert.That(sut.CodeComments[0].LineEnd, Is.EqualTo(66));
        Assert.That(sut.CodeComments[0].ColumnEnd, Is.EqualTo(89));

    }

    [Test]
    public void Line_Without_String_Data_Is_Not_Identified_As_Having_String_Data()
    {
        var codeline = @"	IF @IncludeBlockingSessions = 1 BEGIN ";

        var sut = new Line("build.sql", 346, codeline);

        Assert.False(sut.LineType.HasFlag(LineType.ContainsStrings));
    }

    [Test]
    public void Line_With_Simple_String_Data_Is_Correctly_Flagged_As_Having_String_Data()
    {
        var codeLine = @"		SET @topSQL = REPLACE(@topSQL, N'{blockersUNION} ', @blockersUNION);";

        var sut = new Line("build.sql", 556, codeLine);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsStrings));
    }

    [Test]
    public void Line_With_Simple_String_Data_Correctly_Captures_String_Data()
    {
        var codeLine = @"		SET @topSQL = REPLACE(@topSQL, N'{blockersUNION} ', @blockersUNION);";

        var sut = new Line("build.sql", 556, codeLine);

        StringAssert.AreEqualIgnoringCase(@"N'{blockersUNION} '", sut.CodeStrings[0].Text);
    }

    [Test]
    public void Line_With_Multiple_Strings_Correctly_Captures_All_Full_Strings()
    {
        var codeLine = @"		+ CASE WHEN (SELECT dbo.[get_engine_version]()) > 10.5 THEN N'TRY_CAST' ELSE N'CAST' END + N'(q.[query_plan] AS xml) [statement_plan]' ";

        var sut = new Line("build.sql", 556, codeLine);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsStrings));
        Assert.That(sut.StringType.HasFlag(StringType.SingleLine));

        Assert.That(sut.CodeStrings.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(@"N'TRY_CAST'", sut.CodeStrings[0].Text);
    }
}