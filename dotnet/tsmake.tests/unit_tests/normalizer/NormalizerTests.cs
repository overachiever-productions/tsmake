namespace tsmake.tests.unit_tests.normalizer;

// TODO: 
//     need to add a whole SUITE of tests that verify that CodeLines passed IN to a normalizer are preserved and not modified.
//          and that NEW lines are simply ADDED to what was already there. 

// TODO: verify that we're getting syntax errors when/as applicable as well. 
//   including closures-errors, etc. 

public class NormalizerTests
{
    [Test]
    public void It_Does_Not_Split_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Does_Not_Split_Empty_Strings()
    {
        var text = string.Empty;
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(string.Empty, sut.NormalizedText);
    }

    [Test]
    public void It_Does_Not_Add_LineEndings_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_CrLf()
    {
        var text = "first-line\r\nsecond-line\r\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Lf()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Lf_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Cr()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Cr_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Normalizes_To_Specified_LineEndingOptions()
    {
        var text = "first\r\nsecond\r\nthird\r\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer(LineEndingOptions.Cr);
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\rsecond\rthird\rfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Mixed_Line_Endings()
    {
        var text = "first\r\nsecond\rthird\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\r\nsecond\r\nthird\r\nfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Keeps_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\r\nline2\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Normalized_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\rline2\r";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("line1\r\nline2\r\n", sut.NormalizedText);
    }

    [Test]
    public void It_Calibrates_Line_Endings_Correctly()
    {
        var text = "1\r\n22\r\n333\r\n4444\r\n\r\n666666\r\n7777777\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(8));

        StringAssert.AreEqualIgnoringCase("1", lines[0].Content);
        StringAssert.AreEqualIgnoringCase("22", lines[1].Content);
        StringAssert.AreEqualIgnoringCase("333", lines[2].Content);
        StringAssert.AreEqualIgnoringCase("4444", lines[3].Content);
        StringAssert.AreEqualIgnoringCase("", lines[4].Content);
        StringAssert.AreEqualIgnoringCase("666666", lines[5].Content);
        StringAssert.AreEqualIgnoringCase("7777777", lines[6].Content);
        StringAssert.AreEqualIgnoringCase("", lines[7].Content);

        Assert.That(lines[0].LineNumber, Is.EqualTo(1));
        Assert.That(lines[1].LineNumber, Is.EqualTo(2));
        Assert.That(lines[2].LineNumber, Is.EqualTo(3));
        Assert.That(lines[3].LineNumber, Is.EqualTo(4));
        Assert.That(lines[4].LineNumber, Is.EqualTo(5));
        Assert.That(lines[5].LineNumber, Is.EqualTo(6));
        Assert.That(lines[6].LineNumber, Is.EqualTo(7));
        Assert.That(lines[7].LineNumber, Is.EqualTo(8));

        Assert.That(lines[0].Content.Length, Is.EqualTo(1));
        Assert.That(lines[1].Content.Length, Is.EqualTo(2));
        Assert.That(lines[2].Content.Length, Is.EqualTo(3));
        Assert.That(lines[3].Content.Length, Is.EqualTo(4));
        Assert.That(lines[4].Content.Length, Is.EqualTo(1));
        Assert.That(lines[5].Content.Length, Is.EqualTo(6));
        Assert.That(lines[6].Content.Length, Is.EqualTo(7));
        Assert.That(lines[7].Content.Length, Is.EqualTo(1));
    }
}
