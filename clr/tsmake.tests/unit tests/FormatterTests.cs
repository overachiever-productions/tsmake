
namespace tsmake.tests.unit_tests;

[TestFixture]
public class FormatterTests
{
    //[Test]
    //  assert .. throws if .SetCurrentHostInfo isn't set... 


    [Test]
    public void Percentage_Of_Total_Width_Correctly_Translate_To_Chars()
    {
        var sut = Formatter.Instance;
        sut.SetCurrentHostInfo("Test", 100);

        string dash = sut.Dash(0, TextStyle.None, 80f, 0);

        Assert.That(dash.Length, Is.EqualTo(80));

        sut.SetCurrentHostInfo("Test", 120);
        dash = sut.Dash(0, TextStyle.None, 90f, 0);

        Assert.That(dash.Length, Is.EqualTo(108));
    }

    [Test]
    public void Dash_Returns_Dashed_String_of_Specified_Length()
    {
        var sut = Formatter.Instance;
        sut.SetCurrentHostInfo("Test", 120);

        string dash = sut.Dash(0, TextStyle.None, 90f, 0);

        Assert.That(dash, Is.EqualTo(new string('-', 108)));
    }

    [Test]
    public void Long_Wrapping_Line_Wraps()
    {
        var sut = Formatter.Instance;
        sut.SetCurrentHostInfo("Test", 60);

        string data = new string('-', 130);

        string wrapped = sut.WrappingLine(2, data, TextStyle.None, 80, 2);
        string[] parts = wrapped.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.That(parts.Length, Is.EqualTo(4));
        Assert.That(parts[0], Is.EqualTo("  --------------------------------------------  "));
        Assert.That(parts[1], Is.EqualTo("    ------------------------------------------  "));
        Assert.That(parts[2], Is.EqualTo("    ------------------------------------------  "));
        Assert.That(parts[3], Is.EqualTo("    --  "));
    }

    //[Test]
    //public void Long_Error_Data_Is_Wrapped()
    //{

    //}
}