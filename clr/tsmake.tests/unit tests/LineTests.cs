using tsmake;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class LineTests
{
    [Test]
    public void WhiteSpaceLine_Is_Both_Raw_And_WhiteSpace()
    {
        var sut = new Line("some file.sql", 11, "");

        Assert.False(sut.HasComment);
        Assert.False(sut.HasString);
        Assert.False(sut.IsDirective);
        Assert.False(sut.HasTokens);
    }

    [Test]
    public void Directive_Is_Directive()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line("some file.sql", 11, directiveLine);

        Assert.That(sut.IsDirective);
    }

    [Test]
    public void Directive_Is_Not_WhiteSpace()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line("some file.sql", 11, directiveLine);

        Assert.That(sut.IsDirective);
        Assert.False(sut.HasComment);
        Assert.False(sut.HasTokens);
        Assert.False(sut.HasString);
    }

    [Test]
    public void Non_Comment_Is_Not_Marked_As_Comment()
    {
        string codeLine = @"SELECT @objectId = [object_id], @createDate = create_date FROM master.sys.objects WHERE [name] = N'dba_DatabaseBackups_Log';";

        var sut = new Line("build.sql", 95, codeLine);

        Assert.False(sut.HasComment);
    }

    [Test]
    public void Line_Without_String_Data_Is_Not_Identified_As_Having_String_Data()
    {
        var codeline = @"	IF @IncludeBlockingSessions = 1 BEGIN ";

        var sut = new Line("build.sql", 346, codeline);

        Assert.False(sut.HasString);
    }
}