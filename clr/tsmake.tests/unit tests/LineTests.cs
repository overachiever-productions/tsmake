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
    public void Line_Without_String_Data_Is_Not_Identified_As_Having_String_Data()
    {
        var codeline = @"	IF @IncludeBlockingSessions = 1 BEGIN ";

        var sut = new Line("build.sql", 346, codeline);

        Assert.False(sut.LineType.HasFlag(LineType.ContainsStrings));
    }
}