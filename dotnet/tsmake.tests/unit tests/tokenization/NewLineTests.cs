namespace tsmake.tests.unit_tests.tokenization;

public class NewLineTests
{
    #region Test Strings
    private readonly string SUPER_SIMPLE_MULTI_LINE_STRING = @"New
Line.";

    private readonly string SIMPLE_MULTI_LINE_STRING_WITH_CRLF_TERMINATOR = @"New
Line.
";

    #endregion

    [Test]
    public void NewLineHandlers_Split_String_By_CrLf()
    {
        var sut = Tokenizer.StringTokenizer(SUPER_SIMPLE_MULTI_LINE_STRING);
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(2));

        Assert.That(sut.CodeLines[0].LineNumber, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("New\r\n", sut.CodeLines[0].Text);

        Assert.That(sut.CodeLines[1].LineNumber, Is.EqualTo(2));
        StringAssert.AreEqualIgnoringCase("Line.", sut.CodeLines[1].Text);
    }

    [Test]
    public void NewLineHandlers_Do_Not_Add_Extra_Blank_Lines_At_EoString()
    {
        var sut = Tokenizer.StringTokenizer(SIMPLE_MULTI_LINE_STRING_WITH_CRLF_TERMINATOR);
        sut.Tokenize();

        // should be 3x lines - cuz that's how many there are (NOT 4 lines - i.e., the 'native' CRLF + a bogus/terminator from code/processing.
        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase("", sut.CodeLines[2].Text);
    }

    [Test]
    public void NewLineHandlers_Split_On_LineFeed_Only()
    {
        var sut = Tokenizer.StringTokenizer("This is a\nterrible newline (in windows)\r\n.");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase("This is a\n", sut.CodeLines[0].Text);
        StringAssert.AreEqualIgnoringCase("terrible newline (in windows)\r\n", sut.CodeLines[1].Text);
        StringAssert.AreEqualIgnoringCase(".", sut.CodeLines[2].Text);
    }

    [Test]
    public void NewLineHandlers_Split_On_CarriageReturn_Only()
    {
        var sut = Tokenizer.StringTokenizer("This is a\rterrible newline (in windows)\r\n.");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase("This is a\r", sut.CodeLines[0].Text);
        StringAssert.AreEqualIgnoringCase("terrible newline (in windows)\r\n", sut.CodeLines[1].Text);
        StringAssert.AreEqualIgnoringCase(".", sut.CodeLines[2].Text);
    }

    [Test]
    public void LineHandlers_Identify_Line_Numbers()
    {
        var sut = Tokenizer.StringTokenizer("This is a\rterrible newline (in windows)\r\n.");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(3));

        Assert.That(sut.CodeLines[0].LineNumber, Is.EqualTo(1));
        Assert.That(sut.CodeLines[1].LineNumber, Is.EqualTo(2));
        Assert.That(sut.CodeLines[2].LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void LineHandlers_Identify_Line_StartOffset()
    {
        var sut = Tokenizer.StringTokenizer("This is a\rterrible newline (in windows)\r\n.but this is fine\r\nalso fine.");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(4));
        Assert.That(sut.CodeLines[2].OffsetStart, Is.EqualTo(41));  // NOTE: this IS 'correct' - on a 0-based count. i.e., it's arguable that the line actually STARTS on 42... 
    }
}