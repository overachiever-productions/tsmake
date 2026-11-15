namespace tsmake.tests.unit_tests;

[TestFixture]
public class AssemblerOptionsTests
{
    private static AssemblerOptions NewOptions() => new(TokenDefinitionRegistry.NewRegistry, OperationType.Build);

    #region SetDirectives() - RemoveNone can only be used by itself
    [TestCase(CommentRemovalDirectives.NotSet)]
    [TestCase(CommentRemovalDirectives.RemoveNone)]
    [TestCase(CommentRemovalDirectives.RemoveHeaderComments)]
    [TestCase(CommentRemovalDirectives.RemoveDocComments)]
    [TestCase(CommentRemovalDirectives.RemoveLineEndComments)]
    [TestCase(CommentRemovalDirectives.RemoveBlockComments)]
    [TestCase(CommentRemovalDirectives.RemoveHeaderComments | CommentRemovalDirectives.RemoveLineEndComments)]
    [TestCase(CommentRemovalDirectives.RemoveAllComments)]
    public void Valid_Comment_Removal_Directives_Are_Set(CommentRemovalDirectives directives)
    {
        var options = NewOptions();
        options.SetDirectives(LineEndingsType.Lf, directives, TokenExclusionDirectives.ExcludeStrings);

        Assert.That(options.CommentRemovalDirectives, Is.EqualTo(directives));
        Assert.That(options.LineEndingsType, Is.EqualTo(LineEndingsType.Lf));
        Assert.That(options.TokenExclusionDirectives, Is.EqualTo(TokenExclusionDirectives.ExcludeStrings));
    }

    [TestCase(CommentRemovalDirectives.RemoveHeaderComments)]
    [TestCase(CommentRemovalDirectives.RemoveDocComments)]
    [TestCase(CommentRemovalDirectives.RemoveLineEndComments)]
    [TestCase(CommentRemovalDirectives.RemoveBlockComments)]
    [TestCase(CommentRemovalDirectives.RemoveLineEndComments | CommentRemovalDirectives.RemoveBlockComments)]
    [TestCase(CommentRemovalDirectives.RemoveAllComments)]
    public void RemoveNone_Can_Not_Be_Combined_With_Other_Directives(CommentRemovalDirectives other)
    {
        var options = NewOptions();
        var ex = Assert.Throws<ArgumentException>(() =>
            options.SetDirectives(LineEndingsType.Lf, CommentRemovalDirectives.RemoveNone | other, TokenExclusionDirectives.ExcludeStrings));

        StringAssert.StartsWith("RemoveNone can't be combined with other comment-removal directives.", ex!.Message);
        Assert.That(ex.ParamName, Is.EqualTo("commentRemovalDirectives"));

        // A rejected call changes NOTHING:
        Assert.That(options.CommentRemovalDirectives, Is.EqualTo(CommentRemovalDirectives.RemoveNone));
        Assert.That(options.LineEndingsType, Is.EqualTo(LineEndingsType.CrLf));
        Assert.That(options.TokenExclusionDirectives, Is.EqualTo(TokenExclusionDirectives.None));
    }
    #endregion
}
