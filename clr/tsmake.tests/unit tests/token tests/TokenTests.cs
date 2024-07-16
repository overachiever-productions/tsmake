namespace tsmake.tests.unit_tests.token_tests;

[TestFixture]
public class TokenTests
{
    [Test]
    public void Project_Link_Token_Allows_Uri_As_Default()
    {
        var line = new Line("simplified_build.sql", 27, @"-- {{##PROJECT_LINK:https://www.totalsql.com/tools/xxx}}");

        Assert.That(line.HasTokens);
        Assert.False(line.HasComment);
        Assert.False(line.HasString);
        Assert.That(line.Tokens.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(@"-- {{##PROJECT_LINK:https://www.totalsql.com/tools/xxx}}", line.RawContent);

        var sut = line.Tokens[0];
        Assert.That(sut.Location.Peek().LineNumber, Is.EqualTo(27));
        Assert.That(sut.Location.Peek().FileName, Is.EqualTo("simplified_build.sql"));

        StringAssert.AreEqualIgnoringCase(@"PROJECT_LINK", sut.Name);
        StringAssert.AreEqualIgnoringCase(@"https://www.totalsql.com/tools/xxx", sut.DefaultValue);
    }
}