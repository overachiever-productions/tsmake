using static System.Net.Mime.MediaTypeNames;

namespace tsmake.tests.unit_tests.tokenization;

public class BlockCommentTests
{
    [Test]
    public void BlockCommentHandlers_Match_Simple_Single_Line_BlockComment()
    {
        string text = "SELECT @@SERVERNAME [server_name] /* some comments */\r\rGO";
        var sut = Tokenizer.StringTokenizer(text, new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.BlockComments[0].OffsetStart, Is.EqualTo(34));
        Assert.That(sut.BlockComments[0].OffsetEnd, Is.EqualTo(53));

        // sanity check: 
        string comment = text.Substring(34, 53 - 34);
        StringAssert.AreEqualIgnoringCase(sut.BlockComments[0].Text, comment);


        StringAssert.AreEqualIgnoringCase("/* some comments */", sut.BlockComments[0].Text);
    }

    [Test]
    public void BlockCommentHandlers_Match_Simple_Comments_Across_Multiple_Lines()
    {
        var sut = Tokenizer.StringTokenizer("   /* comments */ SELECT TOP 200\r\n    /*firstname */ last_name   \r\n FROM\r\n/*oldTable*/NewTable;", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase("/* comments */", sut.BlockComments[0].Text);
        StringAssert.AreEqualIgnoringCase("/*firstname */", sut.BlockComments[1].Text);
        StringAssert.AreEqualIgnoringCase("/*oldTable*/", sut.BlockComments[2].Text);
    }

    [Test]
    public void BlockCommentHandlers_Ignore_Stray_Asterixes()
    {
        var sut = Tokenizer.StringTokenizer("/* SELECT * FROM blah;*/\r\nSELECT TOP 200 * FROM blah;", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* SELECT * FROM blah;*/", sut.BlockComments[0].Text);
    }

    [Test]
    public void BlockCommentHandlers_Ignore_Stray_Slashes()
    {
        var sut = Tokenizer.StringTokenizer("\t/* xx /* nested with / and another //// */ */\r\nSELECT @@SERVERNAME [server_name];", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("/* xx /* nested with / and another //// */ */", sut.BlockComments[0].Text);
    }

    [Test]
    public void BlockCommentHandlers_Can_Handle_Simple_Nested_BlockComments()
    {
        var sut = Tokenizer.StringTokenizer("\t/* xx /* nest */ */\r\nSELECT @@SERVERNAME [server_name];", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase("/* xx /* nest */ */", sut.BlockComments[0].Text);
    }

    [Test]
    public void BlockCommentHandlers_Can_Handle_Multiple_Nested_BlockComments()
    {
        var sut = Tokenizer.StringTokenizer("\t/* comment /* sub-comment1 /* sub-comment2 */ */ */\r\nSELECT @@SERVERNAME [server_name];\t/* multi\r\nline block /* sub\r\n comment */\r\n*/", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase("/* comment /* sub-comment1 /* sub-comment2 */ */ */", sut.BlockComments[0].Text);
        StringAssert.AreEqualIgnoringCase("/* multi\r\nline block /* sub\r\n comment */\r\n*/", sut.BlockComments[1].Text);
    }

    [Test]
    public void BlockCommentHandlers_Can_Handle_Adjacent_Nested_Terminators()
    {
        // just a sanity check to make sure code doesn't choke on "*/*/" etc... 
        var sut = Tokenizer.StringTokenizer("\t/* xx /* nest /* nest 2 */*/*/\r\n\tSELECT @@SERVERNAME [server_name];", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
    }

    [Test]
    public void BlockCommentHandlers_Throw_On_Non_Completed_BlockComments()
    {
        var sut = Tokenizer.StringTokenizer("/* this comment is not even close to valid\r\nSELECT TOP 200 * FROM something;", new Stack<string>());
        Assert.Throws<SyntaxException>(sut.Tokenize);
    }

    [Test]
    public void BlockCommentHandlers_Throw_On_Incomplete_Nested_BlockComments()
    {
        var sut = Tokenizer.StringTokenizer("\t/* xx /* nest (but no-nested-close) */ \r\nSELECT @@SERVERNAME [server_name];", new Stack<string>());
        Assert.Throws<SyntaxException>(sut.Tokenize);
    }

    // nesting that doesn't correctly terminate some of the internal /* 

    // need to test the case of double and triple nesting of end comments
    // e.g., "/*  /* comments */*/ 
    // and   "/*   /*   /*   dsaklfjlds */*/*/"


    [Test]
    public void BlockCommentHandlers_Identify_Comment_Start_Line()
    {
        var sut = Tokenizer.StringTokenizer("SELECT * \r\nFROM /* this is a comment */\r\ndbo.someTable;", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.BlockComments[0].StartLine, Is.EqualTo(2));
    }

    [Test]
    public void BlockCommentHandlers_Identify_Comment_Start_Line_Offset()
    {
        var sut = Tokenizer.StringTokenizer("SELECT * \r\nFROM /* this is a \r\n multiline comment */\r\ndbo.someTable;", new Stack<string>());
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(4));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));

        Assert.That(sut.BlockComments[0].StartLine, Is.EqualTo(2));
        Assert.That(sut.BlockComments[0].OffsetStart, Is.Not.EqualTo(sut.CodeLines[1].OffsetStart));
        Assert.That(sut.BlockComments[0].StartLineOffset, Is.EqualTo(sut.CodeLines[1].OffsetStart));
    }
}