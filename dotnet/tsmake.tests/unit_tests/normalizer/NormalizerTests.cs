namespace tsmake.tests.unit_tests.normalizer;

public class NormalizerTests
{
    [Test]
    public void It_Does_Not_Split_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Does_Not_Split_Empty_Strings()
    {
        var text = string.Empty;

        var sut = new Normalizer(text);
        Assert.That(sut.Lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(string.Empty, sut.NormalizedText);
    }

    [Test]
    public void It_Does_Not_Add_LineEndings_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_CrLf()
    {
        var text = "first-line\r\nsecond-line\r\nthird-line";
        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Lf()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Lf_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Cr()
    {
        var text = "first-line\rsecond-line\rthird-line";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Cr_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\rsecond-line\rthird-line";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Normalizes_To_Specified_LineEndingOptions()
    {
        var text = "first\r\nsecond\r\nthird\r\nfourth";

        var sut = new Normalizer(text, LineEndingOptions.Cr);

        Assert.That(sut.Lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\rsecond\rthird\rfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Mixed_Line_Endings()
    {
        var text = "first\r\nsecond\rthird\nfourth";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\r\nsecond\r\nthird\r\nfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Keeps_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\r\nline2\r\n";

        var sut = new Normalizer(text);
        Assert.That(sut.Lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Normalized_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\rline2\r";

        var sut = new Normalizer(text);
        Assert.That(sut.Lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("line1\r\nline2\r\n", sut.NormalizedText);
    }

    [Test]
    public void It_Calibrates_Line_Endings_Correctly()
    {
        var text = "1\r\n22\r\n333\r\n4444\r\n\r\n666666\r\n7777777\r\n";

        var sut = new Normalizer(text);

        Assert.That(sut.Lines.Count, Is.EqualTo(8));

        StringAssert.AreEqualIgnoringCase("1", sut.Lines[0].Text);
        StringAssert.AreEqualIgnoringCase("22", sut.Lines[1].Text);
        StringAssert.AreEqualIgnoringCase("333", sut.Lines[2].Text);
        StringAssert.AreEqualIgnoringCase("4444", sut.Lines[3].Text);
        StringAssert.AreEqualIgnoringCase("", sut.Lines[4].Text);
        StringAssert.AreEqualIgnoringCase("666666", sut.Lines[5].Text);
        StringAssert.AreEqualIgnoringCase("7777777", sut.Lines[6].Text);
        StringAssert.AreEqualIgnoringCase("", sut.Lines[7].Text);

        Assert.That(sut.Lines[0].LineNumber, Is.EqualTo(1));
        Assert.That(sut.Lines[1].LineNumber, Is.EqualTo(2));
        Assert.That(sut.Lines[2].LineNumber, Is.EqualTo(3));
        Assert.That(sut.Lines[3].LineNumber, Is.EqualTo(4));
        Assert.That(sut.Lines[4].LineNumber, Is.EqualTo(5));
        Assert.That(sut.Lines[5].LineNumber, Is.EqualTo(6));
        Assert.That(sut.Lines[6].LineNumber, Is.EqualTo(7));
        Assert.That(sut.Lines[7].LineNumber, Is.EqualTo(8));

        Assert.That(sut.Lines[0].Length, Is.EqualTo(1));
        Assert.That(sut.Lines[1].Length, Is.EqualTo(2));
        Assert.That(sut.Lines[2].Length, Is.EqualTo(3));
        Assert.That(sut.Lines[3].Length, Is.EqualTo(4));
        Assert.That(sut.Lines[4].Length, Is.EqualTo(1));
        Assert.That(sut.Lines[5].Length, Is.EqualTo(6));
        Assert.That(sut.Lines[6].Length, Is.EqualTo(7));
        Assert.That(sut.Lines[7].Length, Is.EqualTo(1));

    }
}
