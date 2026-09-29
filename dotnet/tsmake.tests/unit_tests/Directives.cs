using System.Collections;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class Directives
{
    #region BaseDirective Tests
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
    #endregion

    #region FileDirective Tests
    [Test]
    public void FileDirective_Errors_If_Root_Not_Set()
    {
        var lineText = "--##FILE: Common\\Types\\backup_history_entry.sql  ##:: vNEXT: do something something ";
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("myproject.build.sql", 0, 0));
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, stack);

        var fileSystem = new Mock<IFileSystem>();
        //fileSystem.Setup(fs => fs.RootDirectory).Returns(string.Empty);  // SHOULDN'T be possible ... 
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Throws(new Exception("tsmake Workflow Exception: ROOT Directory has not been set."));

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        Assert.That(directive, Is.TypeOf<FileDirective>());
        Assert.That(directive.IsValid, Is.False);
        Assert.That(directive.ValidationMessage, Is.Not.Null.And.Not.Empty);
        StringAssert.Contains("ROOT Directory has not been set", directive.ValidationMessage);
    }

    [Test]
    public void FileDirective_Is_Not_Valid_If_Path_Not_Found()
    {
        var lineText = "--##FILE: Common\\Types\\backup_history_entry.sql  ##:: vNEXT: do something something ";
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("myproject.build.sql", 0, 0));
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, stack);

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns("D:\\Repositories\\myProject\\Common\\Types\\backup_history_entry.sql");
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        Assert.That(directive, Is.TypeOf<FileDirective>());
        Assert.That(directive.IsValid, Is.False);
        Assert.That(directive.ValidationMessage, Is.Not.Null.And.Not.Empty);
        StringAssert.Contains("specified ##FILE path does not", directive.ValidationMessage);
        StringAssert.Contains("D:\\Repositories\\myProject\\Common\\Types", directive.ValidationMessage);
    }
    #endregion
}