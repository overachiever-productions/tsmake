using tsmake.models.directives;

namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class RootPathDirectiveTests
{
    // TODO: All'z i've got for ROOT Directives is VERY VERY VERY happy-path testing... 

    [Test]
    public void Abosulte_Path_Is_Valid_Root_Directive_Value()
    {
        var line = new Line(2, @"--##ROOT: D:\Dropbox\Repositories\etc.", "build.sql");
        var location = new Location("build.sql", 2, line.Content.IndexOf("ROOT"));
        var sut = new RootPathDirective(line, location);

        Assert.True(sut.IsValid);
    }

    [Test]
    public void Relative_Path_Is_Valid_Root_Directive_Value()
    {
        var line = new Line(2, @"--##ROOT: D:\Dropbox\Repositories\etc.", "build.sql");
        var location = new Location("build.sql", 83, line.Content.IndexOf("ROOT"));
        var sut = new RootPathDirective(line, location);

        Assert.True(sut.IsValid);
    }
}