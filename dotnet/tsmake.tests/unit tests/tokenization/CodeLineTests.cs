namespace tsmake.tests.unit_tests.tokenization;

public class CodeLineTests
{
    [Test]
    public void CodeLines_Correctly_Track_Line_Counts()
    {
        var sut = Tokenizer.StringTokenizer("-- Single Line");
        sut.Tokenize();
        
        Assert.That(sut.CodeLines.Count, Is.EqualTo(1));

        sut = Tokenizer.StringTokenizer("--1\r\n--2\r\n--3\r\n--4");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(4));
    }

    [Test]
    public void CodeLines_Correctly_Track_Line_Numbers()
    {
        var sut = Tokenizer.StringTokenizer("--1\r\n--2\r\n--3\r\n--4");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(4));
        Assert.That(sut.CodeLines[0].LineNumber, Is.EqualTo(1));
        Assert.That(sut.CodeLines[1].LineNumber, Is.EqualTo(2));
        Assert.That(sut.CodeLines[2].LineNumber, Is.EqualTo(3));
        Assert.That(sut.CodeLines[3].LineNumber, Is.EqualTo(4));
    }
}