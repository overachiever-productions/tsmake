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

    #region RootDirective + OutputDirective Tests (which path gets checked)
    [Test]
    public void RootDirective_Checks_The_Directory_Itself()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath("src")).Returns(@"D:\repos\myrepo\src");
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo\src")).Returns(true);

        var directive = LoadDirective("--##ROOT: src", fileSystem.Object);

        Assert.That(directive, Is.TypeOf<RootDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(((RootDirective)directive).Path, Is.EqualTo(@"D:\repos\myrepo\src"));
        fileSystem.Verify(fs => fs.DirectoryExists(@"D:\repos\myrepo"), Times.Never);
    }

    [Test]
    public void RootDirective_Is_Not_Valid_If_Directory_Not_Found_Even_If_Parent_Exists()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath("src")).Returns(@"D:\repos\myrepo\src");
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo")).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo\src")).Returns(false);

        var directive = LoadDirective("--##ROOT: src", fileSystem.Object);

        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("The specified ##ROOT path does not exist", directive.ValidationMessage);
    }

    [Test]
    public void OutputDirective_Checks_The_Directory_The_Artifact_Is_Written_Into()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(@"builds\inclusive.sql")).Returns(@"D:\repos\myrepo\builds\inclusive.sql");
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo\builds")).Returns(true);

        var directive = LoadDirective(@"-- ##OUTPUT: builds\inclusive.sql", fileSystem.Object);

        Assert.That(directive, Is.TypeOf<OutputDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(((OutputDirective)directive).Path, Is.EqualTo(@"D:\repos\myrepo\builds\inclusive.sql"));
        fileSystem.Verify(fs => fs.DirectoryExists(@"D:\repos\myrepo\builds\inclusive.sql"), Times.Never);
    }

    [Test]
    public void OutputDirective_Is_Not_Valid_If_Its_Directory_Not_Found()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns(@"D:\repos\myrepo\builds\inclusive.sql");
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        var directive = LoadDirective(@"-- ##OUTPUT: builds\inclusive.sql", fileSystem.Object);

        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("The directory for the specified ##OUTPUT path does not exist", directive.ValidationMessage);
        StringAssert.Contains(@"D:\repos\myrepo\builds", directive.ValidationMessage);
    }
    #endregion

    #region ROOT / DIRECTORY / OUTPUT against a real FileSystem (throw-away temp directory)
    private static IDirective LoadDirective(string lineText, IFileSystem fileSystem)
    {
        var codeFile = new FileDetail("myproject.build.sql", 0, 0);
        var codeLine = new CodeLine(lineText, 37, 16513, 16558, codeFile);

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        return DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
    }

    private static void WithTempRoot(Action<string, tsmake.FileSystem> test)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            test(temp, new tsmake.FileSystem(temp));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public void Real_RootDirective_Validates_The_Directory_Itself()
    {
        WithTempRoot((temp, fs) =>
        {
            Directory.CreateDirectory(Path.Combine(temp, "src"));

            Assert.That(LoadDirective("--##ROOT: src", fs).IsValid, Is.True);
            Assert.That(LoadDirective("--##ROOT: no_such_folder", fs).IsValid, Is.False);
        });
    }

    [Test]
    public void Real_DirectoryDirective_Is_Not_Valid_For_A_Missing_Folder()
    {
        WithTempRoot((temp, fs) =>
        {
            var directive = LoadDirective("--##DIRECTORY: no_such_folder [sort: ALPHA]", fs);

            Assert.That(directive.IsValid, Is.False);
            StringAssert.Contains("Specified ##DIRECTORY does not exist", directive.ValidationMessage);
        });
    }

    [Test]
    public void Real_DirectoryDirective_Includes_Files_From_An_Existing_Folder()
    {
        WithTempRoot((temp, fs) =>
        {
            string sixes = Path.Combine(temp, "alpha_sixes");
            Directory.CreateDirectory(sixes);
            foreach (var name in new[] { "6a.sql", "6b.sql", "6c.sql", "6d.sql", "6e.sql", "notes.txt" })
                File.WriteAllText(Path.Combine(sixes, name), $"-- {name}");

            var directive = LoadDirective("-- ##DIRECTORY: alpha_sixes [exclude: 6e.sql] [sort: 6d.sql, ALPHA, 6a.sql]", fs);

            Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
            var included = ((DirectoryDirective)directive).IncludedFiles.Select(f => Path.GetFileName(f));
            Assert.That(included, Is.EqualTo(new[] { "6d.sql", "6b.sql", "6c.sql", "6a.sql" }));
        });
    }

    [Test]
    public void Real_OutputDirective_Validates_The_Directory_It_Writes_Into()
    {
        WithTempRoot((temp, fs) =>
        {
            Directory.CreateDirectory(Path.Combine(temp, "builds"));

            Assert.That(LoadDirective("-- ##OUTPUT: inclusive.sql", fs).IsValid, Is.True);              // root itself
            Assert.That(LoadDirective(@"-- ##OUTPUT: builds\inclusive.sql", fs).IsValid, Is.True);
            Assert.That(LoadDirective(@"-- ##OUTPUT: no_such_folder\inclusive.sql", fs).IsValid, Is.False);
        });
    }
    #endregion
}