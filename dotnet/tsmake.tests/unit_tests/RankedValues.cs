using Microsoft.VisualBasic;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class RankedStrings
{
    [Test]
    public void It_Returns_Empty_When_No_Values_Present()
    {
        var strings = new List<IRankedString>();
        
        var rankedValue = RankedString.GetRankedValue(strings);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.Value, Is.EqualTo(string.Empty));
    }

    [Test]
    public void It_Returns_Empty_When_All_Values_Are_Null()
    {
        var strings = new List<IRankedString>();
        strings.Add(new RankedString(SourceType.CommandLine, string.Empty));
        strings.Add(new RankedString(SourceType.ConfigFile, string.Empty));

        var rankedValue = RankedString.GetRankedValue(strings);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.Value, Is.EqualTo(string.Empty));
    }

    [Test]
    public void It_Returns_Highest_Priority_When_Values_Present()
    {
        var buildRoot = new List<IRankedString>();

        // simulate the ROUGH order these'd actually be defined/provided. 
        buildRoot.Add(new RankedString(SourceType.Convention, "D:\\Repositories\\myProject\\")); // Convention = PWD. (not explicit command-line value).
        buildRoot.Add(new RankedString(SourceType.BuildFile, "D:\\Repositories\\myProject\\"));
        buildRoot.Add(new RankedString(SourceType.ConfigFile, "D:\\Repositories\\myProject\\build")); // explicit value from config file supersedes others... 

        var rankedValue = RankedString.GetRankedValue(buildRoot);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.SourceType, Is.EqualTo(SourceType.ConfigFile));
        StringAssert.AreEqualIgnoringCase("D:\\Repositories\\myProject\\build", rankedValue.Value);
    }

    [Test]
    public void It_Does_Not_Care_About_Input_Order()
    {
        // exact same test as [It_Returns_Highest_Priority_When_Values_Present] but with 'random' order: 
        var buildRoot = new List<IRankedString>();

        // simulate the ROUGH order these'd actually be defined/provided. 
        buildRoot.Add(new RankedString(SourceType.ConfigFile, "D:\\Repositories\\myProject\\build")); // explicit value from config file supersedes others... 
        buildRoot.Add(new RankedString(SourceType.Convention, "D:\\Repositories\\myProject\\")); // Convention = PWD. (not explicit command-line value).
        buildRoot.Add(new RankedString(SourceType.BuildFile, "D:\\Repositories\\myProject\\"));

        var rankedValue = RankedString.GetRankedValue(buildRoot);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.SourceType, Is.EqualTo(SourceType.ConfigFile));
        StringAssert.AreEqualIgnoringCase("D:\\Repositories\\myProject\\build", rankedValue.Value);
    }

    [Test]
    public void It_Returns_Highest_Non_Empty_Value_Even_When_Empty_Lower_Values_Present()
    {
        var buildRoot = new List<IRankedString>();
        buildRoot.Add(new RankedString(SourceType.CommandLine, string.Empty));  // should be ignored... 
        buildRoot.Add(new RankedString(SourceType.BuildFile, "D:\\Repositories\\myProject\\"));
        buildRoot.Add(new RankedString(SourceType.ConfigFile, string.Empty));
        buildRoot.Add(new RankedString(SourceType.Convention, string.Empty));

        var rankedValue = RankedString.GetRankedValue(buildRoot);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.SourceType, Is.EqualTo(SourceType.BuildFile));
        StringAssert.AreEqualIgnoringCase("D:\\Repositories\\myProject\\", rankedValue.Value);
    }

    [Test]
    public void It_Returns_CommandLine_Input_Over_Directive_Values()
    {
        var output = new List<IRankedString>();

        output.Add(new RankedString(SourceType.BuildFile, "D:\\Repositories\\myProject\\myproject_old.sql"));
        output.Add(new RankedString(SourceType.CommandLine, "D:\\Repositories\\myProject\\myproject.sql"));

        var rankedValue = RankedString.GetRankedValue(output);

        Assert.That(rankedValue, Is.Not.Null);
        Assert.That(rankedValue.SourceType, Is.EqualTo(SourceType.CommandLine));
        StringAssert.AreEqualIgnoringCase("D:\\Repositories\\myProject\\myproject.sql", rankedValue.Value);
    }

    //[Test]
    //public void It_Returns_Directive_When_Directive_Present()
    //{

    //}
}