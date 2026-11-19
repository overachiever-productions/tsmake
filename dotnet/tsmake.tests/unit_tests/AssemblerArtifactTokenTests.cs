namespace tsmake.tests.unit_tests;

// End-to-end (real FileSystem, throw-away folder): tokens in ##OUTPUT and ##FILE-MARKER -> artifacts -> files written.
[TestFixture]
public class AssemblerArtifactTokenTests
{
    private record Run(Assembler Assembler, BuildResult Result, Dictionary<string, string> Written);

    // %TEMP% in the build file = the throw-away folder. Written: every NEW file in it (and its sub-folders) - by relative path.
    //      dirs: sub-folders to create. callerMarkerPath: a ##FILE-MARKER path passed in by the caller (i.e., no directive).
    private static Run Build(string buildFile, LineEndingsType lineEndings = LineEndingsType.CrLf, Dictionary<string, string>? files = null,
        string[]? dirs = null, string? callerMarkerPath = null, params (string Name, string Value)[] tokens)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var dir in dirs ?? [])
                Directory.CreateDirectory(Path.Combine(temp, dir));
            foreach (var (name, content) in files ?? [])
                File.WriteAllText(Path.Combine(temp, name), content);

            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, buildFile.Replace("%TEMP%", temp));
            var before = Directory.GetFiles(temp, "*", SearchOption.AllDirectories).ToHashSet();

            var registry = TokenDefinitionRegistry.CreateRegistry(tokens.Select(t => new TokenDefinition(t.Name, t.Value)));
            var options = new AssemblerOptions(registry, OperationType.Build);
            options.SetDirectives(lineEndings, CommentRemovalDirectives.RemoveNone, TokenExclusionDirectives.None);
            if (callerMarkerPath is not null)
                options.AddFileMarkerPath(new RankedString(SourceType.CommandLine, callerMarkerPath));

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildPath);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());

            var written = Directory.GetFiles(temp, "*", SearchOption.AllDirectories).Where(f => !before.Contains(f))
                .ToDictionary(f => Path.GetRelativePath(temp, f), File.ReadAllText);
            return new Run(assembler, result, written);
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
                        lineEndings, tokens: [("VERSION", "14.8.5629.2"), ("SUMMARY", "Kill-Store's fixes")]);

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

    #region ##OUTPUT paths: translated AFTER ##ROOT is set and their tokens are replaced
    [Test]
    public void Rooted_Output_Path_Is_Written_Under_Root()
    {
        var run = Build("-- ##ROOT: %TEMP%\\proj\r\n-- ##OUTPUT: ~\\s4_latest.sql\r\nSELECT 1;", dirs: ["proj"]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Assembler.ConfigErrors, Is.Empty);
        Assert.That(run.Written, Is.EqualTo(new Dictionary<string, string> { [@"proj\s4_latest.sql"] = "SELECT 1;" }));
    }

    [Test]
    public void Relative_Output_Path_Is_Written_Next_To_The_Build_File_Even_With_Root()
    {
        var run = Build("-- ##ROOT: %TEMP%\\proj\r\n-- ##OUTPUT: s4_latest.sql\r\nSELECT 1;", dirs: ["proj"]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Written.Keys, Is.EqualTo(new[] { "s4_latest.sql" }));
    }

    [Test]
    public void A_Token_Can_Be_Part_Of_The_Output_Directory()
    {
        var run = Build("-- ##OUTPUT: builds\\{{##VERSION}}\\s4_{{##VERSION}}.sql\r\nSELECT 1;", dirs: [@"builds\14.8"], tokens: [("VERSION", "14.8")]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Assembler.ConfigErrors, Is.Empty);
        Assert.That(run.Written.Keys, Is.EqualTo(new[] { @"builds\14.8\s4_14.8.sql" }));
    }

    [Test]
    public void Missing_Output_Directory_Is_Reported_Against_Its_Line_And_Nothing_Is_Written()
    {
        var run = Build("SET NOCOUNT ON;\r\n-- ##OUTPUT: builds\\{{##VERSION}}\\s4.sql\r\nSELECT 1;", tokens: [("VERSION", "14.8")]);

        var error = run.Assembler.SyntaxErrors.Single();
        Assert.That(error.Message, Is.EqualTo("Invalid --##OUTPUT Directive."));
        Assert.That(error.Detail, Does.StartWith("The directory for the specified ##OUTPUT path does not exist: ").And.EndWith(@"\builds\14.8"));
        Assert.That(error.LineNumber, Is.EqualTo(2));
        Assert.That(run.Written, Is.Empty);
    }

    [Test]
    public void Output_Without_A_Path_Is_Reported()
    {
        var run = Build("-- ##OUTPUT:\r\nSELECT 1;");

        Assert.That(run.Assembler.ConfigErrors.Select(e => e.Message), Does.Contain("Invalid ##Output directive."));
        Assert.That(run.Assembler.SyntaxErrors.Select(e => e.Message), Does.Contain("A ##OUTPUT path is required."));
        Assert.That(run.Written, Is.Empty);
    }
    #endregion

    #region ##FILE-MARKER paths: translated AFTER their tokens are replaced
    [Test]
    public void Relative_Marker_Path_Is_Written_Next_To_The_Build_File()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: {{##VERSION}}.marker.md [body:v{{##VERSION}}]\r\nSELECT 1;", tokens: [("VERSION", "14.8")]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        var marker = run.Result.Artifacts.OfType<FileMarkerArtifact>().Single();
        Assert.That(Path.IsPathRooted(marker.Path), Is.True, marker.Path);
        Assert.That(marker.Path, Does.EndWith(@"\14.8.marker.md"));
        Assert.That(run.Written.Keys, Is.EquivalentTo(new[] { "s4_latest.sql", "14.8.marker.md" }));
        Assert.That(run.Written["14.8.marker.md"], Is.EqualTo("v14.8"));
    }

    [Test]
    public void Rooted_Marker_Path_Is_Written_Under_Root()
    {
        var run = Build("-- ##ROOT: %TEMP%\\proj\r\n-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: ~\\{{##VERSION}}.marker.md\r\nSELECT 1;",
            dirs: ["proj"], tokens: [("VERSION", "14.8")]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Written.Keys, Is.EquivalentTo(new[] { "s4_latest.sql", @"proj\14.8.marker.md" }));
    }

    [Test]
    public void A_Token_Can_Be_Part_Of_The_Marker_Directory()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: releases\\{{##VERSION}}\\s4.marker.md [body:v{{##VERSION}}]\r\nSELECT 1;",
            dirs: [@"releases\14.8"], tokens: [("VERSION", "14.8")]);

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Written[@"releases\14.8\s4.marker.md"], Is.EqualTo("v14.8"));
    }

    [Test]
    public void Missing_Marker_Directory_Is_Reported_Against_Its_Line_And_Nothing_Is_Written()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: releases\\{{##VERSION}}\\s4.marker.md\r\nSELECT 1;", tokens: [("VERSION", "14.8")]);

        var error = run.Assembler.SyntaxErrors.Single();
        Assert.That(error.Message, Is.EqualTo("Invalid --##FILE-MARKER Directive."));
        Assert.That(error.Detail, Does.StartWith("The directory for the specified ##FILE-MARKER path does not exist: ").And.EndWith(@"\releases\14.8"));
        Assert.That(error.LineNumber, Is.EqualTo(2));
        Assert.That(run.Written, Is.Empty);
    }

    [Test]
    public void Undefined_Marker_Path_Token_Is_Reported_Once()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\n-- ##FILE-MARKER: releases\\{{##BRANCH}}\\s4.marker.md\r\nSELECT 1;");

        // Just the undefined token - NOT a 2nd error for the (token-named) directory that doesn't exist.
        var error = run.Assembler.SyntaxErrors.Single();
        StringAssert.StartsWith("Undefined token in the ##FILE-MARKER path: {{##BRANCH}}.", error.Message);
        Assert.That(error.LineNumber, Is.EqualTo(2));
        Assert.That(run.Written, Is.Empty);
    }

    [Test]
    public void Marker_Without_A_Directive_Is_Reported_Not_A_Crash()
    {
        // A path passed in by the caller has no directive (i.e., no line) - which used to throw a NullReferenceException here.
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\nSELECT 1;", callerMarkerPath: @"no_such_folder\s4.marker.md");

        var error = run.Assembler.SyntaxErrors.Single();
        Assert.That(error.Message, Is.EqualTo("Invalid --##FILE-MARKER Directive."));
        Assert.That(error.LineNumber, Is.EqualTo(0));
        Assert.That(error.FileName, Is.EqualTo("(CommandLine)"));
        Assert.That(run.Result.Artifacts.OfType<FileMarkerArtifact>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Marker_Without_A_Directive_Is_Written()
    {
        var run = Build("-- ##OUTPUT: s4_latest.sql\r\nSELECT 1;", callerMarkerPath: "s4.marker.md");

        Assert.That(run.Assembler.SyntaxErrors, Is.Empty);
        Assert.That(run.Written, Is.EqualTo(new Dictionary<string, string> { ["s4_latest.sql"] = "SELECT 1;", ["s4.marker.md"] = "" }));
    }
    #endregion

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
