namespace tsmake.tests.unit_tests;

// End-to-end (real FileSystem, throw-away folder): tokens in ##OUTPUT and ##FILE-MARKER -> artifacts -> files written.
[TestFixture]
public class AssemblerArtifactTokenTests
{
    private record Run(Assembler Assembler, BuildResult Result, Dictionary<string, string> Written);

    // %TEMP% in the build file = the throw-away folder (so ##FILE-MARKER paths can be absolute - i.e., never written anywhere else).
    private static Run Build(string buildFile, LineEndingsType lineEndings = LineEndingsType.CrLf, Dictionary<string, string>? files = null, params (string Name, string Value)[] tokens)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var (name, content) in files ?? [])
                File.WriteAllText(Path.Combine(temp, name), content);

            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, buildFile.Replace("%TEMP%", temp));
            var before = Directory.GetFiles(temp).ToHashSet();

            var registry = TokenDefinitionRegistry.CreateRegistry(tokens.Select(t => new TokenDefinition(t.Name, t.Value)));
            var options = new AssemblerOptions(registry, OperationType.Build);
            options.SetDirectives(lineEndings, CommentRemovalDirectives.RemoveNone, TokenExclusionDirectives.None);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildPath);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());

            var written = Directory.GetFiles(temp).Where(f => !before.Contains(f)).ToDictionary(Path.GetFileName, File.ReadAllText)!;
            return new Run(assembler, result, written!);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestCase(LineEndingsType.CrLf, "\r\n")]
    [TestCase(LineEndingsType.Lf, "\n")]
    public void Tokens_In_Output_And_File_Marker_Are_Replaced_And_Written(LineEndingsType lineEndings, string lineEnding)
    {
        var run = Build("-- ##OUTPUT: s4_{{##VERSION}}.sql\r\n" +
                        "-- ##FILE-MARKER: %TEMP%\\{{##VERSION}}.marker.md [body:# S4 - {{##VERSION}}`r`n`r`n{{##SUMMARY}}]\r\n" +
                        "SELECT 1;",
                        lineEndings, null, ("VERSION", "14.8.5629.2"), ("SUMMARY", "Kill-Store's fixes"));

        Assert.That(run.Assembler.SyntaxErrors.Select(e => $"{e.LineNumber}: {e.Message}"), Is.Empty);
        Assert.That(run.Assembler.ConfigErrors.Select(e => e.Summarize()), Is.Empty);

        Assert.That(run.Result.Artifacts.Select(a => Path.GetFileName(a.Path)), Is.EqualTo(new[] { "s4_14.8.5629.2.sql", "14.8.5629.2.marker.md" }));
        Assert.That(run.Result.Artifacts.Select(a => a.WriteResult?.Exception), Is.All.Null);

        Assert.That(run.Written.Keys, Is.EquivalentTo(new[] { "s4_14.8.5629.2.sql", "14.8.5629.2.marker.md" }));
        Assert.That(run.Written["s4_14.8.5629.2.sql"], Is.EqualTo("SELECT 1;"));
        Assert.That(run.Written["14.8.5629.2.marker.md"], Is.EqualTo($"# S4 - 14.8.5629.2{lineEnding}{lineEnding}Kill-Store's fixes"));
    }

    [Test]
    public void A_Build_Without_Tokens_Is_Written()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\nSELECT 1;");

        Assert.That(run.Assembler.ConfigErrors, Is.Empty);
        Assert.That(run.Result.Artifacts.OfType<BuildArtifact>().Single().WriteResult.Exception, Is.Null);
        Assert.That(run.Written, Is.EqualTo(new Dictionary<string, string> { ["s4_latest.sql"] = "SELECT 1;" }));
    }

    [Test]
    public void A_Build_Without_An_Output_Still_Reports_It()
    {
        var run = Build("SELECT 1;");

        Assert.That(run.Assembler.ConfigErrors.Select(e => e.Message), Is.EqualTo(new[] { "No BUILD Output was specified." }));
        Assert.That(run.Written, Is.Empty);
    }

    [TestCase("a`r`nb", "a\r\nb")]
    [TestCase("a`nb`rc", "a\r\nb\r\nc")]
    [TestCase("`r`na`r`n", "\r\na\r\n")]
    [TestCase("no line breaks", "no line breaks")]
    public void Marker_Body_Line_Breaks_Become_Real_Line_Breaks(string body, string expected)
    {
        var run = Build($"-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: %TEMP%\\s4.marker.md [body:{body}]\r\nSELECT 1;");

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Result.Artifacts.OfType<FileMarkerArtifact>().Single().ArtifactBody, Is.EqualTo(expected));
        Assert.That(run.Written["s4.marker.md"], Is.EqualTo(expected));
    }

    [Test]
    public void Undefined_Output_Token_Is_Reported_Against_Its_Line_And_Nothing_Is_Written()
    {
        var run = Build("SET NOCOUNT ON;\r\n-- ##OUTPUT: s4_{{##VERSION}}.sql\r\nSELECT 1;");

        var error = run.Assembler.SyntaxErrors.Single();
        StringAssert.StartsWith("Undefined token in the ##OUTPUT path: {{##VERSION}}.", error.Message);
        Assert.That(error.LineNumber, Is.EqualTo(2));
        Assert.That(Path.GetFileName(error.FileName), Is.EqualTo("test.build.sql"));
        Assert.That(run.Written, Is.Empty);
    }

    // Tokens are allowed in ##OUTPUT and ##FILE-MARKER ONLY - every other directive still reports them.
    [TestCase("-- ##FILE: {{##VERSION}}.sql")]
    [TestCase("-- ##DIRECTORY: {{##VERSION}}")]
    [TestCase("-- ##FLAG-FILE(#Azure): {{##VERSION}}.sql")]
    public void Tokens_In_Other_Directives_Are_Still_Errors(string directive)
    {
        var run = Build($"-- ##OUTPUT: s4_latest.sql\r\n{directive}\r\nSELECT 1;", files: new() { ["14.8.sql"] = "SELECT 2;" }, tokens: ("VERSION", "14.8"));

        var messages = run.Assembler.SyntaxErrors.Where(e => e.LineNumber == 2).Select(e => e.Message);
        Assert.That(messages, Does.Contain("Tokens can't be used within directives - they are never replaced there."));
    }

    [Test]
    public void Tokens_In_Comments_Are_Not_Errors()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\n-- ##COMMENT: built as v{{##VERSION}}\r\n-- ##:: {{##VERSION}}\r\nSELECT 1;", tokens: ("VERSION", "14.8"));

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
    }
}
