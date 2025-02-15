using static System.Net.Mime.MediaTypeNames;

namespace tsmake.tests.integration_tests.tokenization;

public class ParsingChallenges
{
    [Test]
    public void Eol_Comments_Are_Ignored_When_In_Strings()
    {
        var sut = Tokenizer.StringTokenizer("DECLARE @x nvarchar(MAX) = N'SELECT TOP 200 -- used to be user_ids\r\nuser_name FROM dbo.members;'", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.Comments.Count, Is.EqualTo(0));
    }

    [Test]
    public void Eol_Comments_In_and_Out_Of_Strings_Supported()
    {
        var sut = Tokenizer.StringTokenizer("DECLARE @x nvarchar(MAX) = N'SELECT -- user_ids\r\nuser_name FROM dbo.members;' -- get users\r\nEXEC sp_executesql @x;", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.Comments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("-- get users", sut.Comments[0].Text);
    }

    [Test]
    public void Eol_Comments_Are_Ignored_Within_Block_Comments()
    {
        var sut = Tokenizer.StringTokenizer("/* some old code commented out would look like \r\n\r\n\t\t-- SELECT @blah [blah] \r\n\r\n*/", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.Comments.Count, Is.EqualTo(0));
    }

    [Test]
    public void Eol_Comments_In_and_Out_Of_Block_Comments_Supported()
    {
        var sut = Tokenizer.StringTokenizer("/* some old code commented out would look like \r\n\r\n\t\t-- SELECT @blah [blah] \r\n\r\n*/\r\nSELECT 'meanwhile...'; -- etc\r\n", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.Comments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("-- etc", sut.Comments[0].Text);
    }

    [Test]
    public void Ticks_Within_Eol_Comments_Are_Ignored()
    {
        var sut = Tokenizer.StringTokenizer("DECLARE @oink sysname = N'some string stuff' -- this'z ugly here\r\nSELECT @oink [blah], 'yada' [etc];", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(2));
        Assert.That(sut.Comments.Count, Is.EqualTo(1));
    }

    [Test]
    public void Strings_Within_Eol_Comments_Are_Ignored()
    {
        var sut = Tokenizer.StringTokenizer("DECLARE @oink sysname = N'string' -- ignore 'me' \r\nSELECT @oink [blah], 'yada' [etc];", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(2));
        Assert.That(sut.Comments.Count, Is.EqualTo(1));
    }
}