namespace tsmake.tests.unit_tests;

[TestFixture]
public class ExtensionTests
{
    [Test]
    public void IsValidPath_Returns_True_For_Absolute_Path()
    {
        string path = @"D:\Dropbox\Repositories\dda\deployment\__build";

        Assert.True(path.IsValidPath());
    }
}