namespace tsmake.tests.unit_tests;

[TestFixture]
public class Directives
{
    [Test]
    public void BaseDirective_Trims_Leading_Whitespace_From_Payload()
    {
        var lineText = "--##FILE:  Common\\Types\\backup_history_entry.sql";
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("myproject.build.sql", 0, 0));
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, stack);

        var fileSystem = new Mock<IFileSystem>();
        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        StringAssert.AreEqualIgnoringCase("Common\\Types\\backup_history_entry.sql", directive.Payload);
    }

    [Test]
    public void BaseDirective_Trims_Trailing_Whitespace_From_Payload()
    {
        var lineText = "--##FILE:Common\\Types\\backup_history_entry.sql       ";
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("myproject.build.sql", 0, 0));
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, stack);

        var fileSystem = new Mock<IFileSystem>();
        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        StringAssert.AreEqualIgnoringCase("Common\\Types\\backup_history_entry.sql", directive.Payload);
    }

    [Test]
    public void BaseDirective_Removes_Directive_Comments_From_Payload()
    {
        var lineText = "--##FILE: Common\\Types\\backup_history_entry.sql  ##:: vNEXT: do something something ";
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("myproject.build.sql", 0, 0));
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, stack);

        var fileSystem = new Mock<IFileSystem>();
        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        StringAssert.AreEqualIgnoringCase("Common\\Types\\backup_history_entry.sql", directive.Payload);
    }
}