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
}