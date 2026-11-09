namespace tsmake.tests.unit_tests;

[TestFixture]
public class Directives
{
    #region BaseDirective Tests
    [Test]
    public void BaseDirective_Trims_Leading_Whitespace_From_Payload()
    {
        var lineText = "--##FILE:  Common\\Types\\backup_history_entry.sql";
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

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
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

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
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

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
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

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
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns("D:\\Repositories\\myProject\\Common\\Types\\backup_history_entry.sql");
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);

        Assert.That(directive, Is.Not.Null);
        Assert.That(directive, Is.TypeOf<FileDirective>());
        Assert.That(directive.IsValid, Is.False);
        Assert.That(directive.ValidationMessage, Is.Not.Null.And.Not.Empty);
        StringAssert.Contains("Specified ##FILE does not", directive.ValidationMessage);
        StringAssert.Contains("D:\\Repositories\\myProject\\Common\\Types", directive.ValidationMessage);
    }
    #endregion

    #region DirectoryDirective Tests
    private static IDirective LoadDirectoryDirective(string lineText, Mock<IFileSystem> fileSystem)
    {
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        return DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);
    }

    [Test]
    public void DirectoryDirective_Parses_Path_And_Clauses()
    {
        var lineText = "--##DIRECTORY: Common\\My Tables [exclude: scratch.sql, \"old, unused.sql\"] [sort: 00_setup.sql, ALPHA]  ##:: vNEXT: something";

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath("Common\\My Tables")).Returns("D:\\Repositories\\myProject\\Common\\My Tables");
        fileSystem.Setup(fs => fs.DirectoryExists("D:\\Repositories\\myProject\\Common\\My Tables")).Returns(true);

        var directive = LoadDirectoryDirective(lineText, fileSystem);

        Assert.That(directive, Is.TypeOf<DirectoryDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

        var directory = (DirectoryDirective)directive;
        Assert.That(directory.DirectoryPath, Is.EqualTo("D:\\Repositories\\myProject\\Common\\My Tables"));
        Assert.That(directory.Options, Is.Not.Null);
        Assert.That(directory.Options!.Exclude, Is.EqualTo(new[] { "scratch.sql", "old, unused.sql" }));
        Assert.That(directory.Options.Sort, Is.EqualTo(new[] { "00_setup.sql" }));
        Assert.That(directory.Options.SortMarker, Is.EqualTo(DirectorySortMarker.Alpha));
        Assert.That(directory.Options.SortMarkerPosition, Is.EqualTo(1));
    }

    [Test]
    public void DirectoryDirective_Is_Not_Valid_If_Directory_Not_Found()
    {
        var lineText = "--##DIRECTORY: Common\\Tables [sort: ALPHA]";

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns("D:\\Repositories\\myProject\\Common\\Tables");
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);   // a FILE check must NOT be what makes this valid.
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        var directive = LoadDirectoryDirective(lineText, fileSystem);

        Assert.That(directive, Is.TypeOf<DirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("Specified ##DIRECTORY does not exist", directive.ValidationMessage);
        StringAssert.Contains("D:\\Repositories\\myProject\\Common\\Tables", directive.ValidationMessage);
        Assert.That(((DirectoryDirective)directive).Options, Is.Not.Null);
    }

    [Test]
    public void DirectoryDirective_Is_Not_Valid_If_Syntax_Is_Invalid()
    {
        var lineText = "--##DIRECTORY: Common\\Tables [exlude: scratch.sql]";

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var directive = LoadDirectoryDirective(lineText, fileSystem);

        Assert.That(directive, Is.TypeOf<DirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("Unknown clause '[exlude:'", directive.ValidationMessage);
        Assert.That(((DirectoryDirective)directive).Options, Is.Null);
        Assert.That(((DirectoryDirective)directive).DirectoryPath, Is.Empty);
        fileSystem.Verify(fs => fs.TranslatePath(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DirectoryDirective_Errors_If_Root_Not_Set()
    {
        var lineText = "--##DIRECTORY: Common\\Tables";

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Throws(new Exception("tsmake Workflow Exception: ROOT Directory has not been set."));

        var directive = LoadDirectoryDirective(lineText, fileSystem);

        Assert.That(directive, Is.TypeOf<DirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("ROOT Directory has not been set", directive.ValidationMessage);
    }
    #endregion
}