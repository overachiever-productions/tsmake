namespace tsmake.tests.unit_tests.tokenization;

public class GoStatementTests
{
    [Test]
    public void GoHandlers_Match_Simple_Go_In_Multi_Line_Command()
    {
        string text = "SELECT @@SERVERNAME [server_name];\r\nGO";
        var sut = Tokenizer.StringTokenizer(text);
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        Assert.That(sut.GoStatements[0].OffsetStart, Is.EqualTo(36));
        Assert.That(sut.GoStatements[0].OffsetEnd, Is.EqualTo(38));

        // sanity check: 
        string go = text.Substring(36, 2);
        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, go);

        Assert.That(sut.GoStatements[0].GoCount, Is.EqualTo(0));
        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "GO");
    }

    [Test]
    public void GoHandlers_Correctly_Allow_Spaces_On_Newline_Before_Go()
    {
        var sut = Tokenizer.StringTokenizer("SELECT @@SERVERNAME [server_name];\r\n   GO");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "   GO");
    }

    [Test]
    public void GoHandlers_Correctly_Allow_Spaces_On_Newline_After_Go()
    {
        var sut = Tokenizer.StringTokenizer("SELECT @@SERVERNAME [server_name];\r\n   GO   ");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "   GO");
    }

    [Test]
    public void GoHandlers_Correctly_Allow_Tabs_On_Newline_Before_Go()
    {
        var sut = Tokenizer.StringTokenizer("SELECT @@SERVERNAME [server_name];\r\n\t GO");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "\t GO");
    }

    [Test]
    public void GoHandlers_Correctly_Allow_Eol_Comments_After_Go()
    {
        var sut = Tokenizer.StringTokenizer("SELECT @@SERVERNAME [server_name];\r\n  GO -- with some comment here...");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "  GO");
    }

    [Test]
    public void GoHandlers_Capture_Simple_Go_With_Numbers()
    {
        var sut = Tokenizer.StringTokenizer("CHECKPOINT;\r\nGo   32 -- and a comment");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "Go   32");
        Assert.That(sut.GoStatements[0].GoCount, Is.EqualTo(32));
    }

    [Test]
    public void GoHandlers_Correctly_Allow_EolComments_Touching_Go()
    {
        var sut = Tokenizer.StringTokenizer("SELECT @@SERVERNAME [server_name];\r\n  GO-- this comment is dumb - but legit");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "  GO");
    }

    [Test]
    public void GoHandlers_Correctly_Allow_Numbers_Touching_Go()
    {
        var sut = Tokenizer.StringTokenizer("CHECKPOINT;\r\nGO3\r\nCHECKPOINT;\r\nGO2\r\nSELECT @@SERVERNAME\r\nGO");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[0].Text, "GO3");
        Assert.That(sut.GoStatements[0].GoCount, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[1].Text, "GO2");
        Assert.That(sut.GoStatements[1].GoCount, Is.EqualTo(2));

        StringAssert.AreEqualIgnoringCase(sut.GoStatements[2].Text, "GO");
        Assert.That(sut.GoStatements[2].GoCount, Is.EqualTo(0));
    }

    [Test]
    public void GoHandlers_Ignore_Go_Statements_Within_Block_Comments()
    {
        var sut = Tokenizer.StringTokenizer("/*\r\n\r\nSELECT @@SERVERNAME; \r\nGO \r\n\r\n*/\r\n\r\nSELECT @@VERSION;");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(0));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
    }

    [Test]
    public void GoHandlers_Ignore_Go_Statements_Within_Strings()
    {
        // still not sure why you'd put a "GO" inside of a 'string'... but... don't want it to cause problems IF someone does: 
        var sut = Tokenizer.StringTokenizer("DECLARE @text nvarchar(MAX) = N'/*\r\n\r\nSELECT @@SERVERNAME; \r\nGO \r\n\r\n*/';\r\n\r\nSELECT @@VERSION;\r\nGO");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));
        Assert.That(sut.Strings.Count, Is.EqualTo(1));
    }

    [Test]
    public void GoHandlers_Identify_GoStatement_LineNumber()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'this is\r\nmulti-line-text' [output];\r\nGO");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));
        Assert.That(sut.GoStatements[0].StartLine, Is.EqualTo(3));
    }

    [Test]
    public void GoHandlers_Identify_GoStatement_LineStartOffset()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'this is\r\nmulti-line-text' [output];\r\nGO  -- with some comments");
        sut.Tokenize();

        Assert.That(sut.GoStatements.Count, Is.EqualTo(1));
        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));

        Assert.That(sut.GoStatements[0].StartLine, Is.EqualTo(3));

        Assert.That(sut.GoStatements[0].StartLineOffset, Is.EqualTo(45));
        StringAssert.AreEqualIgnoringCase(sut.CodeLines[2].Text, "GO  -- with some comments");

        // sanity checks: 
        StringAssert.AreEqualIgnoringCase(sut.RawText.Substring(45,2), "GO");
    }
}