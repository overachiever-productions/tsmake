using tsmake.data_models;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class TokenDefinitionRegistryTests
{
    private class CustomTokenDefinition(string name, string specifiedValue) : ITokenDefinition
    {
        public string Name { get; } = name;
        public string SpecifiedValue { get; set; } = specifiedValue;
    }

    [Test]
    public void CreateRegistry_AddsConcreteAndInterfaceTokenDefinitions()
    {
        var concrete = new TokenDefinition("first", "one");
        List<ITokenDefinition> tokens = [concrete, new CustomTokenDefinition("second", " two ")];

        var registry = TokenDefinitionRegistry.CreateRegistry(tokens);

        Assert.That(registry.DefinedTokens.Count, Is.EqualTo(2));
        Assert.That(registry.GetTokenDefinition("##FIRST"), Is.SameAs(concrete));
        Assert.That(registry.GetTokenDefinition("##SECOND").SpecifiedValue, Is.EqualTo("two"));
    }

    [Test]
    public void CreateRegistry_UsesSetTokenOverwriteBehaviorForDuplicates()
    {
        List<ITokenDefinition> tokens = [new TokenDefinition("name", "first"), new CustomTokenDefinition("##NAME", "second")];

        var registry = TokenDefinitionRegistry.CreateRegistry(tokens);

        Assert.That(registry.DefinedTokens.Count, Is.EqualTo(1));
        Assert.That(registry.GetTokenDefinition("##NAME").SpecifiedValue, Is.EqualTo("second"));
    }

    [Test]
    public void CreateRegistry_WithEmptyTokens_CreatesIndependentEmptyRegistry()
    {
        var registry = TokenDefinitionRegistry.CreateRegistry([]);
        var otherRegistry = TokenDefinitionRegistry.NewRegistry;
        registry.SetToken(new TokenDefinition("name", "value"));

        Assert.That(otherRegistry.DefinedTokens, Is.Empty);
        Assert.That(registry.DefinedTokens.Count, Is.EqualTo(1));
    }

    [Test]
    public void CreateRegistry_RejectsNullCollectionAndNullToken()
    {
        Assert.Throws<ArgumentNullException>(() => TokenDefinitionRegistry.CreateRegistry(null!));
        Assert.Throws<ArgumentNullException>(() => TokenDefinitionRegistry.CreateRegistry([null!]));
    }
}
