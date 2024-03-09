namespace tsmake.tests.unit_tests;

[TestFixture]
public class ExtensionTests
{
    [Test]
    public void IsValidPath_Returns_True_For_Absolute_Path()
    {
        string path = @"C:\Windows\System32";

        Assert.True(path.IsValidPath());
    }

    [Test]
    public void CollapsePath_Removes_Parent_Path_When_Directed()
    {
        string path = @"C:\Windows\System32\drivers";

        var test = path.CollapsePath(@"..\");

        StringAssert.AreEqualIgnoringCase(@"C:\Windows\System32", test);
    }

    [Test]
    public void CollapsePath_Joins_Paths_As_Directed()
    {
        string path = @"C:\Windows\System32\drivers";

        var test = path.CollapsePath(@"..\Microsoft");

        StringAssert.AreEqualIgnoringCase(@"C:\Windows\System32\Microsoft", test);
    }
}