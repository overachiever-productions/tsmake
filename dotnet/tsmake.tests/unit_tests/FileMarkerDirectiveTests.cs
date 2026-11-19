namespace tsmake.tests.unit_tests;

// ##FILE-MARKER: FileMarkerParser wired into the FileMarkerDirective -> DirectiveProcessor -> FileMarkerArtifact.
//      (FileMarkerParser's own syntax rules are covered by FileMarkerParserTests.)
[TestFixture]
public class FileMarkerDirectiveTests
{
    private const string Body = @"![](https://assets.overachiever.net/s4/images/s4_main_logo.png)`r`n`r`n# S4 - {version}`r`n`r`n{summary}`r`n`r`nSee the [change log](/changelog.md).";

    private static IDirective LoadDirective(string lineText, IFileSystem fileSystem)
    {
        var codeLine = new CodeLine(lineText, 3, 100, 100 + lineText.Length, new FileDetail("myproject.build.sql", 0, 0));
        var m = Global.DirectiveRegex.Match(codeLine.OriginalContent);
        return DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
    }

    #region FileMarkerDirective
    [Test]
    public void Directive_With_Path_Only()
    {
        var directive = LoadDirective(@"-- ##FILE-MARKER: ~\{version}.marker.md", new Mock<IFileSystem>().Object);

        Assert.That(directive, Is.TypeOf<FileMarkerDirective>());
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

        var marker = (FileMarkerDirective)directive;
        Assert.That(marker.Path, Is.EqualTo(@"~\{version}.marker.md"));
        Assert.That(marker.PathType, Is.EqualTo(PathType.Rooted));
        Assert.That(marker.Body, Is.Null);
        Assert.That(marker.Options, Is.EqualTo(new FileMarkerOptions(@"~\{version}.marker.md", null)));
    }

    [Test]
    public void Directive_With_Body()
    {
        var directive = (FileMarkerDirective)LoadDirective($@"--##FILE-MARKER: D:\Builds\S4 {{version}}.md [body:{Body}] ##:: a note", new Mock<IFileSystem>().Object);

        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(directive.Path, Is.EqualTo(@"D:\Builds\S4 {version}.md"));
        Assert.That(directive.PathType, Is.EqualTo(PathType.Absolute));
        Assert.That(directive.Body, Is.EqualTo(Body));

        // The Payload is still the WHOLE payload (minus the ##:: comment) - i.e., path + [body: ...].
        Assert.That(directive.Payload, Is.EqualTo($@"D:\Builds\S4 {{version}}.md [body:{Body}]"));
    }

    [Test]
    public void Path_Is_Not_Translated_Or_Checked_Yet()
    {
        // {placeholders} have to be swapped out BEFORE the path can be translated/validated - so the file system isn't touched (yet).
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var directive = (FileMarkerDirective)LoadDirective(@"-- ##FILE-MARKER: ~\{version}\marker.md [body: hello]", fileSystem.Object);

        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);
        Assert.That(directive.Path, Is.EqualTo(@"~\{version}\marker.md"));
    }

    [TestCase(@"-- ##FILE-MARKER: ~\x.md [body: hello", "missing its closing ']'")]
    [TestCase(@"-- ##FILE-MARKER: ~\x.md [body:]", "The ##FILE-MARKER [body:] clause is empty.")]
    [TestCase(@"-- ##FILE-MARKER: [body: hello]", "A ##FILE-MARKER path is required before the [body: ...] clause.")]
    [TestCase(@"-- ##FILE-MARKER: ", "A ##FILE-MARKER path is required.")]
    [TestCase(@"-- ##FILE-MARKER: ~\x.md [bdy: hello]", "Unknown clause '[bdy:'.")]
    public void Invalid_Syntax_Makes_Directive_Invalid(string line, string expectedMessage)
    {
        var directive = (FileMarkerDirective)LoadDirective(line, new Mock<IFileSystem>().Object);

        Assert.That(directive.IsValid, Is.False);
        StringAssert.Contains(expectedMessage, directive.ValidationMessage);
        Assert.That(directive.Options, Is.Null);
        Assert.That(directive.Path, Is.Empty);
        Assert.That(directive.Body, Is.Null);
    }
    #endregion

    #region FileMarkerArtifact
    [Test]
    public void Artifact_Body_Comes_From_Its_Directive()
    {
        var directive = LoadDirective(@"-- ##FILE-MARKER: ~\x.md [body: hello]", new Mock<IFileSystem>().Object);
        var artifact = new FileMarkerArtifact(@"~\x.md", SourceType.BuildFile, directive);

        Assert.That(artifact.ArtifactBody, Is.EqualTo("hello"));
    }

    [Test]
    public void Artifact_Without_A_Directive_Has_No_Body()
    {
        var artifact = new FileMarkerArtifact("marker.md", SourceType.CommandLine, null!);

        Assert.That(artifact.ArtifactBody, Is.Null);
    }
    #endregion

    #region Assembler (end-to-end: build file -> FileMarkerPath -> FileMarkerArtifact)
    private static (Assembler Assembler, BuildResult Result) Build(string buildFile)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, buildFile);

            var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildPath);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());
            return (assembler, result);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public void Build_File_Marker_With_Body_Becomes_An_Artifact()
    {
        var (assembler, result) = Build($"-- ##OUTPUT: test_latest.sql\r\n--##FILE-MARKER: ~\\{{version}}.marker.md [body:{Body}]\r\nSELECT 1;");

        Assert.That(assembler.ConfigErrors.Select(e => e.Summarize()), Is.Empty);
        Assert.That(assembler.SyntaxErrors.Select(e => e.Message), Is.Empty);

        // The ranked value is the PATH only - not the whole payload:
        Assert.That(assembler.FileMarkerPath.Select(r => r.Value), Is.EqualTo(new[] { @"~\{version}.marker.md" }));

        // ... and the artifact's path is TRANSLATED (~\ => ##ROOT: the throw-away folder):
        var marker = result.Artifacts.OfType<FileMarkerArtifact>().Single();
        Assert.That(Path.IsPathRooted(marker.Path), Is.True, marker.Path);
        Assert.That(marker.Path, Does.EndWith(@"\{version}.marker.md").And.Contains("tsmake_tests_"));
        Assert.That(marker.SourceType, Is.EqualTo(SourceType.BuildFile));
        Assert.That(marker.ArtifactBody, Is.EqualTo(Body.Replace("`r`n", "\r\n")));      // `r`n => real line breaks (the build's: CrLf by default).
    }

    [Test]
    public void Build_File_Marker_Without_Body_Becomes_An_Artifact()
    {
        var (assembler, result) = Build("-- ##OUTPUT: test_latest.sql\r\n-- ##FILE-MARKER: ~\\{version}.marker.md\r\nSELECT 1;");

        Assert.That(assembler.ConfigErrors, Is.Empty);
        var marker = result.Artifacts.OfType<FileMarkerArtifact>().Single();
        Assert.That(marker.Path, Does.EndWith(@"\{version}.marker.md").And.Contains("tsmake_tests_"));
        Assert.That(marker.ArtifactBody, Is.Null);
    }

    [Test]
    public void Invalid_Build_File_Marker_Is_Reported_And_Not_An_Artifact()
    {
        var (assembler, result) = Build("-- ##OUTPUT: test_latest.sql\r\n-- ##FILE-MARKER: ~\\x.md [body: oops\r\nSELECT 1;");

        Assert.That(result.Artifacts.OfType<FileMarkerArtifact>(), Is.Empty);
        Assert.That(assembler.FileMarkerPath, Is.Empty);

        var error = assembler.ConfigErrors.Single();
        Assert.That(error.Message, Is.EqualTo("Invalid ##FileMarker directive."));
        StringAssert.Contains("missing its closing ']'", error.Detail);
    }
    #endregion
}
