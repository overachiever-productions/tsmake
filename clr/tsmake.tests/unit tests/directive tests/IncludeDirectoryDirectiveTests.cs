using NUnit.Framework.Internal;
using tsmake.models.directives;

namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class IncludeDirectoryDirectiveTests
{
    //[Test]
    //public void This_is_not_a_test()
    //{
    //    // it's an option for me to debug and see what's going on. 

    //    var line = new Line(2, "-- ## DIRECTORY: Common ORDERBY: alpha EXCLUDE: %tables% PRIORITIES: get_engine_version%, %split_string%; get_s4_version%", "build.sql");
    //    var location = new Location("build.sql", 83, line.Content.IndexOf("DIRECTORY"));

    //    var sut = new IncludeDirectoryDirective("DIRECTORY", line, location);

    //    Assert.True(sut.IsValid);
    //}


    [Test]
    public void Simple_Directory_Directive_Extracts_Directory_Name()
    {
        var line = new Line(12, "-- ## DIRECTORY: Common ", "build.sql");
        var location = new Location("build.sql", 83, line.Content.IndexOf("DIRECTORY", StringComparison.Ordinal));

        var sut = new IncludeDirectoryDirective(line, location);

        Assert.True(sut.IsValid);
        Assert.That(sut.Path, Is.EqualTo("Common"));
    }

    [Test]
    public void Simple_Directory_Directive_Identifies_Directory_Path_Type()
    {
        var line = new Line(12, "-- ## DIRECTORY: Common ", "build.sql");
        var location = new Location("build.sql", 83, line.Content.IndexOf("DIRECTORY", StringComparison.Ordinal));

        var sut = new IncludeDirectoryDirective(line, location);

        Assert.True(sut.IsValid);
        Assert.That(sut.PathType, Is.EqualTo(PathType.Relative));
    }

}