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

        StringAssert.AreEqualIgnoringCase(@"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' ", sut.CodeOnlyText);
    }

    [Test]
    public void Simple_Comment_Captures_Comment_Text()
    {
        // TODO: I'm not 100% sure I want this to capture/preserve the RAW comment - I MIGHT want the comment text WITHOUT -- ... 
        //      then again, I might not... 

        var codeLine = @"DECLARE @CurrentVersion varchar(20) = N'{{##S4version:oink}}' -- this is a simple comment ";

        var sut = new Line("build.sql", 66, codeLine);

        StringAssert.AreEqualIgnoringCase(@"-- this is a simple comment ", sut.GetCommentText());
    }

    [Test]
    public void Simple_BlockComment_Is_Marked_As_BlockComment()
    {
        var codeline = @"DECLARE @version sysname;  /* this is a simple block comment */    ";

        var sut = new Line("build.sql", 70, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));
    }

    [Test]
    public void Simple_BlockComment_At_End_Of_Line_Is_Marked_Eol()
    {
        var codeline = @"DECLARE @version sysname;  /* this is a simple block comment */    ";

        var sut = new Line("build.sql", 70, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
    }

    [Test]
    public void Simple_Midline_BlockComment_Is_Marked_As_MidLine()
    {
        var codeline = @"	        @server = /* sample line with midline comment */ N'PARTNER', ";

        var sut = new Line("build.sql", 406, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));
    }

    [Test]
    public void Multiple_BlockComments_On_Single_Line_Are_Marked_Correctly()
    {
        var codeline = @"	        @server = /* sample line with */ N'PARTNER',   /* multiple block comments */";

        var sut = new Line("build.sql", 406, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultipleSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));
    }

    [Test]
    public void Simple_BlockComment_With_Comment_And_Whitespace_Only_Is_Marked_Correctly()
    {
        var codeline = @"       /* TODO: fix the stuff down below.... */    "; // 2x tabs in the front of this line. 

        var sut = new Line("build.sql", 502, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.WhiteSpaceAndComment));
    }

    [Test]
    public void Multiple_BlockComments_And_Whitespace_Only_On_Single_Line_Is_Marked_Correctly()
    {
        var codeline = @"/* this too has multiple */  /* block comments - but it's only */ /* whitespace otherwise */";

        var sut = new Line("build.sql", 552, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultipleSingleLineComments));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.WhiteSpaceAndComment));
    }

    [Test]
    public void Simple_BlockComment_Start_Only_Is_Correctly_Identified()
    {
        var codeline = @"	INSERT INTO dbo.version_history (version_number, [description], deployed)  /* this is the START of a block-comment ... ";

        var sut = new Line("build.sql", 445, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
    }

    [Test]
    public void Simple_BlockComment_Start_Is_Also_Flagged_As_Eol_Comment()
    {
        var codeline = @"	INSERT INTO dbo.version_history (version_number, [description], deployed)  /* this is the START of a block-comment ... ";

        var sut = new Line("build.sql", 445, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
    }

    [Test]
    public void BlockComment_Start_On_Line_With_Complete_BlockComments_Is_Marked_Correctly()
    {
        var codeline = @"/* this is a block comment sample */ SELECT @@SERVERNAME /* Where ";

        var sut = new Line("build.sql", 423, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        // TODO: this is true ... but ... confusing and ... I'm not sure it matters ... 
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MidlineComment));
        
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultipleSingleLineComments));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.EolComment));
    }

    [Test]
    public void Whitespace_Only_BlockComment_Start_Is_Marked_Start_And_Whitespace_Only()
    {
        var codeline = @"   /* white space and block-comment starts are a thing too... ";

        var sut = new Line("build.sql", 431, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultipleSingleLineComments));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.WhiteSpaceAndComment));
    }

    [Test]
    public void Multiple_Whitespace_And_Block_Comments_Only_Plus_Start_Are_Marked_Correctly()
    {
        var codeline = @"  /* it's also possible */ /* for multiple block-comments to ";

        var sut = new Line("build.sql", 431, codeline);

        Assert.That(sut.LineType.HasFlag(LineType.ContainsComments));
        Assert.That(sut.CommentType.HasFlag(CommentType.BlockComment));

        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.NestedSingleLineComments));
        Assert.False(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineNested));

        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultipleSingleLineComments));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.MultiLineStart));
        Assert.That(sut.BlockCommentType.HasFlag(BlockCommentType.WhiteSpaceAndComment));
    }
}