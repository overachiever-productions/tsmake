namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class RootPathDirectiveTests
{
    // TODO: All'z i've got for ROOT Directives is VERY VERY VERY happy-path testing... 
    // TODO: also... these paths are very MY-MACHINE oriented - i.e., If I'm going to publish theses tests with tsmake - paths have to be more 'universal'. 
    //      and... need to address what to do about folks running these tests on Mac, Linux, etc... 
    //              i.e., probably need various bits of logic within the tests based on Enviroment.Platform/OS... 

    [Test]
    public void Absolute_Path_Is_Valid_Root_Directive_Value()
    {
        var line = new Line("build.sql", 2, @"--##ROOT: D:\Dropbox\Repositories\etc.");
        
        var sut = line.Directive;

        Assert.True(sut.IsValid);
    }

    [Test]
    public void Relative_Path_Is_Valid_Root_Directive_Value()
    {
        var line = new Line("build.sql", 2, @"--##ROOT: D:\Dropbox\Repositories\etc.");

        var sut = line.Directive;

        Assert.True(sut.IsValid);
    }
}