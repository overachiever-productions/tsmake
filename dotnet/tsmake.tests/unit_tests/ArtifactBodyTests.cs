namespace tsmake.tests.unit_tests;

// Serializing code-lines into a single string (tsmakeExtensions.Serialize()) - and the Assembler's ArtifactBody (which uses it).
[TestFixture]
public class ArtifactBodyTests
{
    private static readonly FileDetail FileName = new("file-name.sql", 0, 0);

    private static ICodeLine Line(string original, string? transformed = null) =>
        new CodeLine(original, 1, 0, 0, FileName) { TransformedContent = transformed };

    private static List<ICodeLine> Lines(params string[] lines) => lines.Select(l => Line(l)).ToList();

    #region Serialize()
    [Test]
    public void No_Lines_Serialize_To_An_Empty_String()
    {
        Assert.That(new List<ICodeLine>().Serialize(LineEndingsType.CrLf), Is.EqualTo(string.Empty));
    }

    [Test]
    public void A_Single_Line_Gets_No_Line_Ending()
    {
        Assert.That(Lines("SELECT 1;").Serialize(LineEndingsType.CrLf), Is.EqualTo("SELECT 1;"));
    }

    [TestCase(LineEndingsType.CrLf, "A\r\nB\r\nC")]
    [TestCase(LineEndingsType.Lf, "A\nB\nC")]
    [TestCase(LineEndingsType.Cr, "A\rB\rC")]
    public void Lines_Are_Joined_With_The_Specified_Line_Endings(LineEndingsType lineEndings, string expected)
    {
        Assert.That(Lines("A", "B", "C").Serialize(lineEndings), Is.EqualTo(expected));
    }

    [Test]
    public void Empty_Lines_Are_Preserved()
    {
        Assert.That(Lines("", "A", "", "").Serialize(LineEndingsType.CrLf), Is.EqualTo("\r\nA\r\n\r\n"));
    }

    [Test]
    public void Transformed_Content_Is_Used_When_Present()
    {
        var lines = new List<ICodeLine>
        {
            Line("original 1"),
            Line("original 2", "transformed 2"),
            Line("original 3", ""),                 // an EMPTY transformation is still a transformation.
            Line("original 4")
        };

        Assert.That(lines.Serialize(LineEndingsType.CrLf), Is.EqualTo("original 1\r\ntransformed 2\r\n\r\noriginal 4"));
    }

    // Normalize() splits a file into code-lines; serializing them gives back the file - with normalized line endings.
    [Test]
    public void Serialized_Lines_Match_The_Normalized_Text(
        [Values("", "SELECT 1;", "\r\n", "SELECT 1;\r\n", "\r\nSELECT 1;", "A\r\nB\nC\rD", "A\n\r B", "A\r\n\r\n\n\rB\r\n\r\n")] string text,
        [Values] LineEndingsType lineEndings)
    {
        var normalizer = new Normalizer(lineEndings);
        var lines = new List<ICodeLine>();
        normalizer.Normalize(text, lines, new List<ISyntaxError>(), FileName);

        Assert.That(lines.Serialize(lineEndings), Is.EqualTo(normalizer.NormalizedText));
    }

    [Test]
    public void Null_Lines_Are_Rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ((List<ICodeLine>)null!).Serialize(LineEndingsType.CrLf));
    }

    [Test]
    public void Unknown_Line_Endings_Are_Rejected()
    {
        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(() => ((LineEndingsType)99).ToLineEnding());
    }
    #endregion

    #region Assembler.ArtifactBody
    private static Assembler Build(string buildFile, Dictionary<string, string> files, LineEndingsType lineEndings, params (string Name, string Value)[] tokens)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var (name, content) in files)
                File.WriteAllText(Path.Combine(temp, name), content);

            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, buildFile);

            var registry = TokenDefinitionRegistry.CreateRegistry(tokens.Select(t => new TokenDefinition(t.Name, t.Value)));
            var options = new AssemblerOptions(registry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            options.SetDirectives(lineEndings, CommentRemovalDirectives.RemoveNone, TokenExclusionDirectives.None);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildPath);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());
            Assert.That(assembler.SyntaxErrors.Select(e => $"{e.LineNumber}: {e.Message}"), Is.Empty);
            return assembler;
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestCase(LineEndingsType.CrLf, "\r\n")]
    [TestCase(LineEndingsType.Lf, "\n")]
    [TestCase(LineEndingsType.Cr, "\r")]
    public void The_Artifact_Body_Is_The_Build_Output_With_Tokens_Replaced(LineEndingsType lineEndings, string lineEnding)
    {
        var files = new Dictionary<string, string>
        {
            ["child.sql"] = "PRINT N'{{##SUMMARY}}';\nSELECT '{{##UNDEFINED}}';",     // (mixed line endings in the source files.)
        };

        var assembler = Build("SET NOCOUNT ON; -- v{{##VERSION}}\r\n-- ##OUTPUT: test_latest.sql\r-- ##FILE: child.sql\r\nPRINT 'done';", files, lineEndings,
            ("SUMMARY", "Lots of fixin' and stuff"), ("VERSION", "7.1.2"));

        string expected = string.Join(lineEnding,
            "SET NOCOUNT ON; -- v7.1.2",
            "PRINT N'Lots of fixin'' and stuff';",
            "SELECT '{{##UNDEFINED}}';",
            "PRINT 'done';");

        Assert.That(assembler.ArtifactBody, Is.EqualTo(expected));
        Assert.That(assembler.ArtifactBody, Is.EqualTo(string.Join(lineEnding, assembler.ArtifactLines.Select(l => l.GetContent()))));
    }

    [Test]
    public void A_Trailing_Line_Break_Is_Kept()
    {
        var assembler = Build("SET NOCOUNT ON;\r\n-- ##OUTPUT: test_latest.sql\r\nPRINT 'done';\r\n", new Dictionary<string, string>(), LineEndingsType.CrLf);

        Assert.That(assembler.ArtifactBody, Is.EqualTo("SET NOCOUNT ON;\r\nPRINT 'done';\r\n"));
    }
    #endregion
}
