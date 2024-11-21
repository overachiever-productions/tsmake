namespace tsmake.tests.unit_tests.tokenization;

public class StringTests
{
    [Test]
    public void StringHandlers_Identify_Very_Simple_String()
    {
        var sut = Tokenizer.StringTokenizer("This has a 'string' in it.");
        sut.Tokenize();

        Assert.That(sut.CodeLines.Count, Is.EqualTo(1));

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        Assert.That(sut.Strings[0].OffsetStart, Is.EqualTo(11));
        Assert.That(sut.Strings[0].OffsetEnd, Is.EqualTo(18));

        StringAssert.AreEqualIgnoringCase("'string'", sut.Strings[0].Text);
    }

    [Test]
    public void StringHandlers_Identify_Ascii_String_As_Ascii()
    {
        var sut = Tokenizer.StringTokenizer("This is 'ascii'.");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        Assert.That(sut.Strings[0].IsUnicode, Is.False);
    }

    [Test]
    public void StringHandlers_Identify_Unicode_String_As_Unicode()
    {
        var sut = Tokenizer.StringTokenizer("This is not N'ascii'.");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        Assert.That(sut.Strings[0].IsUnicode, Is.True);
    }

    [Test]
    public void StringHandlers_Identify_Strings_Spanning_Multiple_Lines()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'This \r\nstring spans\r\nmultiple lines' [test_case];");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        Assert.That(sut.Strings[0].IsUnicode, Is.False);

        StringAssert.AreEqualIgnoringCase("'This \r\nstring spans\r\nmultiple lines'", sut.Strings[0].Text);
    }

    [Test]
    public void StringHandlers_Identify_Multiple_Strings_In_Single_Line()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'Simple String' as [test1], N'unicode' [test2];");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(2));
    }

    [Test]
    public void StringHandlers_Identify_Multiple_Strings_Across_Many_Lines()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'This \r\nstring spans\r\nmultiple lines' [test_case], N'And another string\r\ntoo' [test_case2], N'test 3' [single_line_test];");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(3));
        Assert.That(sut.Strings[0].IsUnicode, Is.False);
        Assert.That(sut.Strings[1].IsUnicode, Is.True);
        Assert.That(sut.Strings[2].IsUnicode, Is.True);

        StringAssert.AreEqualIgnoringCase("'This \r\nstring spans\r\nmultiple lines'", sut.Strings[0].Text);
        StringAssert.AreEqualIgnoringCase("N'And another string\r\ntoo'", sut.Strings[1].Text);
        StringAssert.AreEqualIgnoringCase("N'test 3'", sut.Strings[2].Text);
    }

    [Test]
    public void StringHandlers_Identify_Strings_At_End_Of_Text()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'this is a simple string.'");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("'this is a simple string.'", sut.Strings[0].Text);
    }

    [Test]
    public void StringHandlers_Can_Handle_Simple_Escaped_Tick()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'There''s a tick in here.'");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("'There''s a tick in here.'", sut.Strings[0].Text);
    }

    [Test]
    public void StringHandlers_Can_Handle_Escaped_Strings()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'This has a ''nested string'' in it.'");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase("'This has a ''nested string'' in it.'", sut.Strings[0].Text);
    }

    [Test]
    public void StringHandlers_Can_Handle_Nesting_And_Other_Strings()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'This \r\nstring spans\r\nmultiple lines' [test_case], N'So does this string but ''with\r\nnested'' ticks' [test_case2], N'test 3' [single_line_test];");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(3));

        StringAssert.AreEqualIgnoringCase("'This \r\nstring spans\r\nmultiple lines'", sut.Strings[0].Text);
        StringAssert.AreEqualIgnoringCase("N'So does this string but ''with\r\nnested'' ticks'", sut.Strings[1].Text);
        StringAssert.AreEqualIgnoringCase("N'test 3'", sut.Strings[2].Text);
    }

    [Test]
    public void StringHandlers_Throw_Exception_On_Non_Completed_String()
    {
        var sut = Tokenizer.StringTokenizer("SELECT 'This has a bad string in it");

        Assert.Throws<SyntaxException>(sut.Tokenize);
    }

    [Test]
    public void StringHandlers_Identify_LineNumber_Of_Strings()
    {
        var sut = Tokenizer.StringTokenizer("--1\r\n--2\r\n--3\r\n'4'");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));

        var s1 = sut.Strings[0];
        Assert.That(s1.StartLine, Is.EqualTo(4));
        Assert.That(s1.OffsetStart, Is.EqualTo(15));
        Assert.That(s1.StartLineOffset, Is.EqualTo(15));  
        Assert.That(s1.ColumnStart, Is.EqualTo(1));
    }

    [Test]
    public void StringHandlers_Identify_ColumnStart_Of_Strings()
    {
        var sut = Tokenizer.StringTokenizer("--1\r\nSELECT 'some string' [output]");
        sut.Tokenize();

        Assert.That(sut.Strings.Count, Is.EqualTo(1));

        var s1 = sut.Strings[0];
        Assert.That(s1.StartLine, Is.EqualTo(2));
        Assert.That(s1.ColumnStart, Is.EqualTo(8));
    }

    // with -- and 'string' in the comments. but ... don't break the line. 
    //          i.e., think it's as simple as adding a new EolCommentStatus ... and IF tokenizer.EolStatus <> None ... then ignore... 

    // with the OTHER scenario I had a problem with (that caused this shit-show detour in the first place).
}