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

    #region FlagFileDirective + FlagDirectoryDirective Tests
    private const string FlagDirectoryLine = @"-- ##FLAG-DIRECTORY(#Azure): ~\azure-funcs\ [sort: azure_helper.sql, ALPHA] [exclude: _ignored.sql] (DEFAULT): ~\on-prem-funcs\ [sort: ALPHA]  ";
    private const string AzurePayload = @"~\azure-funcs\ [sort: azure_helper.sql, ALPHA] [exclude: _ignored.sql]";
    private const string OnPremPayload = @"~\on-prem-funcs\ [sort: ALPHA]";

    private static Mock<IFileSystem> FlagDirectoryFileSystem(bool azureExists = true, bool onPremExists = true)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(@"~\azure-funcs\")).Returns(@"D:\repos\myrepo\azure-funcs\");
        fileSystem.Setup(fs => fs.TranslatePath(@"~\on-prem-funcs\")).Returns(@"D:\repos\myrepo\on-prem-funcs\");
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo\azure-funcs\")).Returns(azureExists);
        fileSystem.Setup(fs => fs.DirectoryExists(@"D:\repos\myrepo\on-prem-funcs\")).Returns(onPremExists);
        return fileSystem;
    }

    [TestCase(@"-- ##FLAG-FILE(DEFAULT): onprem-sixes.sql (#Azure): azure-sixes.sql")]
    [TestCase(@"-- ##FLAG-FILE(#RDS, #Azure): azure-sixes.sql (DEFAULT): onprem-sixes.sql")]
    [TestCase(@"-- ##FLAGS-FILE(#On_Prem-2) : onprem-sixes.sql")]
    public void FlagFileDirective_Is_Loaded_For_Any_Valid_First_Option(string lineText)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns<string>(p => @"D:\repos\myrepo\" + p);
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

        var directive = LoadDirective(lineText, fileSystem.Object);

        Assert.That(directive, Is.TypeOf<FlagFileDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
    }

    [TestCase(@"-- ##FLAG-FILE(#Azure): azure-sixes.sql (DEFAULT): onprem-sixes.sql ##:: some note")]
    [TestCase(@"-- ##FLAG-FILE(#Azure): azure-sixes.sql (DEFAULT): onprem-sixes.sql  ##:: vNEXT: (#RDS): rds-sixes.sql")]
    [TestCase(@"-- ##FLAG-FILE(#Azure): azure-sixes.sql (DEFAULT): onprem-sixes.sql##::no spaces")]
    public void FlagFileDirective_Ignores_Directive_Comments(string lineText)
    {
        // ONLY the exact (comment-free) paths exist - so a ##:: comment left in a path makes the directive invalid.
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns<string>(p => @"D:\repos\myrepo\" + p);
        fileSystem.Setup(fs => fs.FileExists(@"D:\repos\myrepo\azure-sixes.sql")).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(@"D:\repos\myrepo\onprem-sixes.sql")).Returns(true);

        var directive = (FlagFileDirective)LoadDirective(lineText, fileSystem.Object);

        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(directive.ResolveOptionFromFlags(new List<string> { "#Azure" })!.Path, Is.EqualTo("azure-sixes.sql"));
        Assert.That(directive.ResolveOptionFromFlags(new List<string> { "#RDS" })!.Path, Is.EqualTo("onprem-sixes.sql"));
    }

    [TestCase(FlagDirectoryLine)]
    [TestCase(@"--##FLAGS-DIRECTORY(#Azure): ~\azure-funcs\ (DEFAULT): ~\on-prem-funcs\")]
    [TestCase(@"  --   ##flag-directory ( #azure ) : ~\azure-funcs\ (default): ~\on-prem-funcs\")]
    [TestCase(@"-- ##FLAG-DIRECTORY(#RDS, #Azure): ~\azure-funcs\ (DEFAULT): ~\on-prem-funcs\")]
    [TestCase(@"-- ##FLAG-DIRECTORY(DEFAULT): ~\on-prem-funcs\ (#Azure): ~\azure-funcs\")]
    public void FlagDirectoryDirective_Is_Valid_When_Every_Option_Directory_Exists(string lineText)
    {
        var directive = LoadDirective(lineText, FlagDirectoryFileSystem().Object);

        Assert.That(directive, Is.TypeOf<FlagDirectoryDirective>());
        Assert.That(directive.DirectiveType, Is.EqualTo(DirectiveType.FlagDirectory));
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
    }

    [Test]
    public void FlagDirectoryDirective_Is_Not_Valid_If_Any_Option_Directory_Is_Missing()
    {
        var directive = LoadDirective(FlagDirectoryLine, FlagDirectoryFileSystem(azureExists: false).Object);

        Assert.That(directive, Is.TypeOf<FlagDirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains(@"Directory specified for (#AZURE) does not exist: D:\repos\myrepo\azure-funcs\", directive.ValidationMessage);
        StringAssert.DoesNotContain("DEFAULT", directive.ValidationMessage);
    }

    [Test]
    public void FlagDirectoryDirective_Is_Not_Valid_If_An_Option_Has_Invalid_Directory_Syntax()
    {
        var fileSystem = FlagDirectoryFileSystem();
        var directive = LoadDirective(@"-- ##FLAG-DIRECTORY(#Azure): ~\azure-funcs\ [sort: ALPHA, TIMESTAMP] (DEFAULT): ~\on-prem-funcs\", fileSystem.Object);

        Assert.That(directive, Is.TypeOf<FlagDirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("Invalid ##DIRECTORY details for (#Azure):", directive.ValidationMessage);
        StringAssert.Contains("Only one sort marker", directive.ValidationMessage);
        fileSystem.Verify(fs => fs.TranslatePath(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void FlagDirectoryDirective_Without_Options_Is_Reported_Not_Ignored()
    {
        var directive = LoadDirective(@"-- ##FLAG-DIRECTORY: ~\on-prem-funcs\", FlagDirectoryFileSystem().Object);

        Assert.That(directive, Is.TypeOf<FlagDirectoryDirective>());
        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains("Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-DIRECTORY", directive.ValidationMessage);
    }

    [Test]
    public void FlagDirectoryDirective_Ignores_Directive_Comments()
    {
        var directive = LoadDirective(FlagDirectoryLine + @" ##:: vNEXT: (#RDS): ~\rds-funcs\", FlagDirectoryFileSystem().Object);

        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

        var option = ((IFlagIncludeResolver)directive).ResolveOptionFromFlags(new List<string> { "#RDS" });
        Assert.That(option!.Key, Is.EqualTo("DEFAULT"));
        Assert.That(option.Path, Is.EqualTo(OnPremPayload));
    }

    [TestCase("#Azure")]
    [TestCase("#AZURE")]
    [TestCase("#azure")]
    public void FlagDirectoryDirective_Resolves_The_Option_For_A_Matching_Flag(string flag)
    {
        var directive = (FlagDirectoryDirective)LoadDirective(FlagDirectoryLine, FlagDirectoryFileSystem().Object);

        var option = directive.ResolveOptionFromFlags(new List<string> { "#RDS", flag });

        Assert.That(option, Is.Not.Null);
        Assert.That(option!.Key, Is.EqualTo(flag));
        Assert.That(option.Path, Is.EqualTo(AzurePayload));
    }

    [Test]
    public void FlagDirectoryDirective_Resolves_To_Default_Without_A_Matching_Flag()
    {
        var directive = (FlagDirectoryDirective)LoadDirective(FlagDirectoryLine, FlagDirectoryFileSystem().Object);

        foreach (var flags in new[] { new List<string>(), new List<string> { "#RDS" } })
        {
            var option = directive.ResolveOptionFromFlags(flags);

            Assert.That(option, Is.Not.Null);
            Assert.That(option!.Key, Is.EqualTo("DEFAULT"));
            Assert.That(option.Path, Is.EqualTo(OnPremPayload));
        }
    }

    [Test]
    public void FlagDirectoryDirective_Resolves_To_Nothing_Without_A_Matching_Flag_Or_Default()
    {
        var directive = (FlagDirectoryDirective)LoadDirective(@"-- ##FLAG-DIRECTORY(#Azure): ~\azure-funcs\", FlagDirectoryFileSystem().Object);

        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(directive.ResolveOptionFromFlags(new List<string> { "#RDS" }), Is.Null);
        Assert.That(directive.ResolveOptionFromFlags(new List<string>()), Is.Null);
    }

    [Test]
    public void FlagDirectoryDirective_Uses_The_First_Of_The_Flags_That_Has_An_Option()
    {
        var directive = (FlagDirectoryDirective)LoadDirective(@"-- ##FLAG-DIRECTORY(#Azure): ~\azure-funcs\ (#RDS): ~\on-prem-funcs\", FlagDirectoryFileSystem().Object);

        Assert.That(directive.ResolveOptionFromFlags(new List<string> { "#RDS", "#Azure" })!.Path, Is.EqualTo(@"~\on-prem-funcs\"));
        Assert.That(directive.ResolveOptionFromFlags(new List<string> { "#Azure", "#RDS" })!.Path, Is.EqualTo(@"~\azure-funcs\"));
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

    // ##OUTPUT's path is NOT translated or checked when the directive is loaded (##ROOT isn't known yet, and the path can contain tokens):
    //      BuildArtifact.TranslatePath() does both - AFTER ##ROOT is set and tokens are replaced (see AssemblerArtifactTokenTests).
    [TestCase(@"builds\inclusive.sql", PathType.Relative)]
    [TestCase(@"~\builds\inclusive.sql", PathType.Rooted)]
    [TestCase(@"D:\builds\{{##VERSION}}\inclusive.sql", PathType.Absolute)]
    public void OutputDirective_Keeps_The_Path_As_Specified(string path, PathType expectedPathType)
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);      // i.e., the file system isn't touched.

        var directive = LoadDirective($"-- ##OUTPUT: {path}", fileSystem.Object);

        Assert.That(directive, Is.TypeOf<OutputDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(((OutputDirective)directive).Path, Is.EqualTo(path));
        Assert.That(((OutputDirective)directive).PathType, Is.EqualTo(expectedPathType));
    }

    [TestCase("-- ##OUTPUT:")]
    [TestCase("-- ##OUTPUT:    ")]
    [TestCase("-- ##OUTPUT: ##:: just a note")]
    public void OutputDirective_Requires_A_Path(string lineText)
    {
        var directive = LoadDirective(lineText, new Mock<IFileSystem>(MockBehavior.Strict).Object);

        Assert.That(directive.IsValid, Is.False);
        Assert.That(directive.ValidationMessage, Is.EqualTo("A ##OUTPUT path is required."));
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
    public void Real_BuildArtifact_Validates_The_Directory_It_Writes_Into()
    {
        WithTempRoot((temp, fs) =>
        {
            Directory.CreateDirectory(Path.Combine(temp, "builds"));

            foreach (var (path, valid) in new[] { ("inclusive.sql", true), (@"builds\inclusive.sql", true), (@"~\builds\inclusive.sql", true), (@"no_such_folder\inclusive.sql", false) })
            {
                var directive = (OutputDirective)LoadDirective($"-- ##OUTPUT: {path}", fs);
                var artifact = new BuildArtifact(directive.Path, SourceType.BuildFile, directive);
                var errors = new List<ISyntaxError>();

                artifact.TranslatePath(fs, errors);

                if (valid)
                {
                    Assert.That(errors, Is.Empty, path);
                    Assert.That(artifact.Path, Is.EqualTo(Path.Combine(temp, path.Replace(@"~\", ""))), path);     // root = the build file's folder, here.
                }
                else
                {
                    var error = errors.Single();
                    Assert.That(error.Message, Is.EqualTo("Invalid --##OUTPUT Directive."));
                    Assert.That(error.Detail, Is.EqualTo($"The directory for the specified ##OUTPUT path does not exist: {Path.Combine(temp, "no_such_folder")}"));
                    Assert.That(error.LineNumber, Is.EqualTo(37));
                    Assert.That(artifact.Path, Is.EqualTo(path));       // left as-is.
                }
            }
        });
    }

    private static void WriteFlagDirectoryFiles(string temp)
    {
        foreach (var (folder, names) in new[] {
            ("azure-funcs", new[] { "a_func.sql", "_ignored.sql", "azure_helper.sql", "b_func.sql", "notes.txt" }),
            ("on-prem-funcs", new[] { "d_func.sql", "c_func.sql" }) })
        {
            string directory = Path.Combine(temp, folder);
            Directory.CreateDirectory(directory);
            foreach (var name in names)
                File.WriteAllText(Path.Combine(directory, name), $"-- {name}");
        }
    }

    [TestCase("#Azure", new[] { "azure_helper.sql", "a_func.sql", "b_func.sql" })]
    [TestCase("#RDS", new[] { "c_func.sql", "d_func.sql" })]
    public void Real_FlagDirectoryDirective_Resolves_To_The_Directory_Selected_By_Flags(string flag, string[] expected)
    {
        WithTempRoot((temp, fs) =>
        {
            WriteFlagDirectoryFiles(temp);

            var directive = LoadDirective(FlagDirectoryLine, fs);
            Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

            // Same as the DirectiveProcessor: the resolved option becomes a ##DIRECTORY.
            var option = ((IFlagIncludeResolver)directive).ResolveOptionFromFlags(new List<string> { flag });
            var directory = new DirectoryDirective(option!.Path, directive.CodeLine, fs);

            Assert.That(directory.IsValid, Is.True, directory.ValidationMessage);
            Assert.That(directory.IncludedFiles.Select(f => Path.GetFileName(f)), Is.EqualTo(expected));
        });
    }

    [Test]
    public void Real_FlagDirectoryDirective_Is_Not_Valid_For_A_Missing_Folder()
    {
        WithTempRoot((temp, fs) =>
        {
            Directory.CreateDirectory(Path.Combine(temp, "on-prem-funcs"));

            var directive = LoadDirective(FlagDirectoryLine, fs);

            Assert.That(directive.IsValid, Is.False);
            StringAssert.Contains("Directory specified for (#AZURE) does not exist", directive.ValidationMessage);
        });
    }

    [TestCase("#Azure", new[] { "azure_helper.sql", "a_func.sql", "b_func.sql" })]
    [TestCase(null, new[] { "c_func.sql", "d_func.sql" })]
    public void Real_Build_Includes_The_Directory_Selected_By_Flags(string? flag, string[] expected)
    {
        WithTempRoot((temp, fs) =>
        {
            WriteFlagDirectoryFiles(temp);
            string buildFile = Path.Combine(temp, "flags.build.sql");
            File.WriteAllText(buildFile, $"-- ##OUTPUT: flags_latest.sql\r\n{FlagDirectoryLine}\r\nSELECT 'after the flag-directory';\r\n");

            var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            options.SetDirectives(LineEndingsType.CrLf, CommentRemovalDirectives.RemoveNone, TokenExclusionDirectives.None);
            if (flag != null)
                options.SetFlags(new List<string> { flag }, FlagsSource.Console);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(fs, options), options, result);
            assembler.Assemble(buildFile);

            Assert.That(result.Exception, Is.Null);
            Assert.That(assembler.SyntaxErrors.Select(e => e.ToString()), Is.Empty);

            var included = assembler.ArtifactLines
                .Select(l => l.FileDetail.FilePath)
                .Where(p => !string.Equals(p, buildFile, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Select(p => Path.GetFileName(p));
            Assert.That(included, Is.EqualTo(expected));
        });
    }
    #endregion
}