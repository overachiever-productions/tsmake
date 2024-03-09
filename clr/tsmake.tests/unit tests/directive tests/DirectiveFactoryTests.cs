using NUnit.Framework;

namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class DirectiveFactoryTests
{
    [Test]
    public void Root_Directive_String_Returns_Root_Directive()
    {
        var line = new Line("build.sql", 2, @"-- ## ROOT: ..\\ ##:: and this is a comment");
        
        Assert.NotNull(line.Directive);
        Assert.That(line.Directive.DirectiveName, Is.EqualTo("ROOT"));
    }

    [Test]
    public void Output_Directive_String_Returns_Output_Directive()
    {
        var line = new Line("build.sql", 3, @"-- ## OUTPUT: \\\piggly_wiggly.sql");
        Assert.NotNull(line.Directive);
        Assert.That(line.Directive.DirectiveName, Is.EqualTo("OUTPUT"));
    }
}