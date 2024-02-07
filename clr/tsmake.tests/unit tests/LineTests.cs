using tsmake.enums;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class LineTests
{
    [Test]
    public void WhiteSpaceLine_Is_Both_Raw_And_WhiteSpace()
    {
        string emptyLine = "";
        var sut = new Line(11, emptyLine, "some file.sql");

        Assert.That(sut.LineType.HasFlag(LineType.RawContent));
        Assert.That(sut.LineType.HasFlag(LineType.WhitespaceOnly));
    }

    [Test]
    public void Directive_Is_Directive()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line(11, directiveLine, "some file.sql");

        Assert.That(sut.LineType.HasFlag(LineType.Directive));
    }

    [Test]
    public void Directive_Is_Not_WhiteSpace()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line(11, directiveLine, "some file.sql");

        // wth?
        Assert.False(sut.LineType.HasFlag(LineType.WhitespaceOnly));
    }

    [Test]
    public void Directive_Is_Not_RawContent()
    {
        string directiveLine = @"--##OUTPUT: \\Deployment";

        var sut = new Line(11, directiveLine, "some file.sql");

        // wth?
        Assert.False(sut.LineType.HasFlag(LineType.RawContent));
    }
}