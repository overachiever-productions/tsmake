namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class IncludeFileDirectiveTests
{
    [Test]
    public void Illegal_Windows_Characters_Cause_IsValid_To_Be_False()
    {
        var line = new Line("build.sql", 452, @"--##FILE: this isn't correct?.");

        var sut = line.Directive;

        Assert.False(sut.IsValid);
    }

    [Test]
    public void Illegal_Windows_Characters_Cause_ValidationMessage_To_Populate()
    {
        var line = new Line("build.sql", 452, @"--##FILE: this isn't correct?.");

        var sut = line.Directive;
        Assert.IsNotEmpty(sut.ValidationMessage);
    }
}