namespace tsmake.tests.unit_tests;

[TestFixture]
public class TokenDefinitionTests
{
    private class CustomTokenDefinition(string name, string specifiedValue) : ITokenDefinition
    {
        public string Name { get; } = name;
        public string SpecifiedValue { get; set; } = specifiedValue;
    }

    #region Names
    [TestCase("COPYRIGHT", "##COPYRIGHT")]
    [TestCase("##COPYRIGHT", "##COPYRIGHT")]
    [TestCase("##Copyright", "##COPYRIGHT")]               // definitions are case-insensitive (tokens in .sql files are NOT).
    [TestCase("project-site", "##PROJECT-SITE")]
    [TestCase("  ##V2  ", "##V2")]
    [TestCase("S4VERSION-SUMMARY", "##S4VERSION-SUMMARY")]
    [TestCase("A-B-C", "##A-B-C")]
    [TestCase("123", "##123")]
    public void Valid_Names_Are_Stored_Upper_Case_With_A_Preamble(string name, string expected)
    {
        Assert.That(TokenDefinition.GetValidationError(name, "x"), Is.Null);
        Assert.That(new TokenDefinition(name, "x").Name, Is.EqualTo(expected));
    }

    [Test]
    public void A_Null_Name_Is_Rejected()
    {
        Assert.That(TokenDefinition.GetValidationError(null, "x"), Is.EqualTo("A token name is required."));
        Assert.Throws<ArgumentException>(() => new TokenDefinition(null!, "x"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("##")]
    [TestCase("#COPYRIGHT")]
    [TestCase("####COPYRIGHT")]
    [TestCase("COPY##RIGHT")]
    [TestCase("COPY RIGHT")]
    [TestCase("COPY_RIGHT")]                                // no underscores.
    [TestCase("COPY--RIGHT")]                               // single dashes only ...
    [TestCase("-COPYRIGHT")]                                // ... BETWEEN letters/numbers.
    [TestCase("COPYRIGHT-")]
    [TestCase("COPYRIGHT:")]
    [TestCase("{{##COPYRIGHT}}")]
    [TestCase("ÇOPYRIGHT")]                                 // A-Z only.
    public void Invalid_Names_Are_Rejected(string name)
    {
        string? error = TokenDefinition.GetValidationError(name, "x");
        Assert.That(error, Is.Not.Null);

        var ex = Assert.Throws<ArgumentException>(() => new TokenDefinition(name, "x"));
        Assert.That(ex!.Message, Does.StartWith(error));
    }
    #endregion

    #region Values
    [TestCase("", "")]
    [TestCase("   ", "")]                                   // values are trimmed.
    [TestCase("  7.1.2  ", "7.1.2")]
    [TestCase("Lots of fixin' and stuff: $5 - {{##X}}", "Lots of fixin' and stuff: $5 - {{##X}}")]
    public void Valid_Values_Are_Trimmed(string value, string expected)
    {
        Assert.That(new TokenDefinition("X", value).SpecifiedValue, Is.EqualTo(expected));
    }

    [TestCase("line\r\nbreak")]
    [TestCase("line\nbreak")]
    [TestCase("line\rbreak")]
    [TestCase("trailing line break\n")]                     // (checked BEFORE trimming.)
    public void Values_With_Line_Breaks_Are_Rejected(string value)
    {
        StringAssert.Contains("can't contain line breaks", TokenDefinition.GetValidationError("X", value));
        Assert.Throws<ArgumentException>(() => new TokenDefinition("X", value));

        var definition = new TokenDefinition("X", "ok");
        Assert.Throws<ArgumentException>(() => definition.SpecifiedValue = value);
        Assert.That(definition.SpecifiedValue, Is.EqualTo("ok"));
    }

    [Test]
    public void A_Null_Value_Is_Rejected()
    {
        StringAssert.Contains("needs a value", TokenDefinition.GetValidationError("X", null));
        Assert.Throws<ArgumentException>(() => new TokenDefinition("X", null!));
    }
    #endregion

    #region CreateRegistry (does NOT trust its callers)
    [TestCase("bad name", "x")]
    [TestCase("COPY_RIGHT", "x")]
    [TestCase("GOOD", "multi\r\nline")]
    public void CreateRegistry_Rejects_Invalid_Definitions(string name, string value)
    {
        List<ITokenDefinition> tokens = [new TokenDefinition("FINE", "fine"), new CustomTokenDefinition(name, value)];

        Assert.Throws<ArgumentException>(() => TokenDefinitionRegistry.CreateRegistry(tokens));
    }

    [Test]
    public void CreateRegistry_Normalizes_Custom_Definitions()
    {
        var registry = TokenDefinitionRegistry.CreateRegistry([new CustomTokenDefinition("##Project-Site", "  https://x  ")]);

        Assert.That(registry.DefinedTokens.Keys, Is.EqualTo(new[] { "##PROJECT-SITE" }));
        Assert.That(registry.GetTokenDefinition("##PROJECT-SITE").SpecifiedValue, Is.EqualTo("https://x"));
    }
    #endregion
}
