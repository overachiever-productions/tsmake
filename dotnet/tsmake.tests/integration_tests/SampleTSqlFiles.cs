namespace tsmake.tests.integration_tests;

// Regression: REAL (non-test) T-SQL files - test_files\sample-t-sql-files - assembled as ##DIRECTORY includes:
//      WITHOUT and WITH RemoveHeaderComments, and WITH token definitions (all builds run once, in OneTimeSetUp).
[TestFixture]
public class SampleTSqlFiles
{
    private const string Copyright = "Copyright (c) 2026 OverAchiever Productions, LLC.";

    private string _samplesFolder = string.Empty;
    private Assembler _kept = null!;        // CommentRemovalDirectives.RemoveNone
    private Assembler _removed = null!;     // CommentRemovalDirectives.RemoveHeaderComments
    private Assembler _tokens = null!;      // {{##COPYRIGHT}} defined - plus 'decoys' named like S4's own (runtime) {TOKENS}.

    [OneTimeSetUp]
    public void BuildSamples()
    {
        _samplesFolder = FindSamplesFolder();
        _kept = Build(CommentRemovalDirectives.RemoveNone);
        _removed = Build(CommentRemovalDirectives.RemoveHeaderComments);
        _tokens = Build(CommentRemovalDirectives.RemoveNone, TokenDefinitionRegistry.CreateRegistry([
            new TokenDefinition("COPYRIGHT", Copyright),
            new TokenDefinition("SYSTEM", "decoy"), new TokenDefinition("ALL", "decoy"), new TokenDefinition("DEFAULT", "decoy")]));
    }

    // Walks up from the test binaries (...\dotnet\tsmake.tests\bin\Debug\netX) to the repo's test_files - so no hard-coded paths.
    private static string FindSamplesFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "test_files", "sample-t-sql-files");
            if (Directory.Exists(candidate))
                return candidate;
        }

        Assert.Fail($"Could not find test_files\\sample-t-sql-files above {AppContext.BaseDirectory}.");
        return string.Empty;
    }

    private Assembler Build(CommentRemovalDirectives removal, ITokenDefinitionRegistry? tokens = null)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // NOTE: code BEFORE the directives - otherwise they'd be the build file's header comments (and removed along with it).
            string buildFile = Path.Combine(temp, "samples.build.sql");
            File.WriteAllText(buildFile, $"SET NOCOUNT ON;\r\n-- ##OUTPUT: samples_latest.sql\r\n-- ##DIRECTORY: \"{_samplesFolder}\"\r\n");

            var options = new AssemblerOptions(tokens ?? TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            options.SetDirectives(LineEndingsType.CrLf, removal, TokenExclusionDirectives.None);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildFile);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());
            Assert.That(assembler.SyntaxErrors.Select(e => $"{Path.GetFileName(e.FileName)} line {e.LineNumber}: {e.Message}"), Is.Empty);
            return assembler;
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private IEnumerable<string> SampleFiles() =>
        Directory.GetFiles(_samplesFolder, "*.sql", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)!).OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

    private static List<ICodeLine> LinesFrom(Assembler assembler, string fileName) =>
        assembler.ArtifactLines.Where(l => Path.GetFileName(l.FileDetail.FilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase)).ToList();

    private static IEnumerable<string> Numbered(IEnumerable<ICodeLine> lines) => lines.Select(l => $"{l.LineNumber}: {l.OriginalContent}");

    [Test]
    public void Without_Header_Removal_Every_Sample_File_Is_Included_In_Full()
    {
        Assert.That(SampleFiles(), Is.Not.Empty);

        foreach (var fileName in SampleFiles())
        {
            string[] fileLines = Regex.Split(File.ReadAllText(Path.Combine(_samplesFolder, fileName)), @"\r\n|\r|\n");

            Assert.That(LinesFrom(_kept, fileName).Select(l => l.OriginalContent), Is.EqualTo(fileLines), fileName);
        }
    }

    // Holds for ANY file added to the folder: everything from the first code on is kept - as-is, with its original line numbers.
    [Test]
    public void With_Header_Removal_Everything_From_The_First_Code_On_Is_Kept()
    {
        foreach (var fileName in SampleFiles())
        {
            var all = LinesFrom(_kept, fileName);
            var kept = LinesFrom(_removed, fileName);

            Assert.That(kept.Count, Is.LessThanOrEqualTo(all.Count), fileName);
            if (kept.Count == 0)
                continue;       // the whole file is header comments.

            var original = all[all.Count - kept.Count];       // the line where code starts.

            // the first kept line: same line - minus (at most) a block comment that ended on it.
            Assert.That(kept[0].LineNumber, Is.EqualTo(original.LineNumber), fileName);
            StringAssert.EndsWith(kept[0].OriginalContent, original.OriginalContent, fileName);

            string code = kept[0].OriginalContent.TrimStart();
            Assert.That(code, Is.Not.Empty, $"{fileName}: the first kept line is blank.");
            Assert.That(code.StartsWith("--") || code.StartsWith("/*"), Is.False, $"{fileName}: the first kept line is a comment: {code}");

            // ... and everything after it: identical.
            Assert.That(Numbered(kept.Skip(1)), Is.EqualTo(Numbered(all.Skip(all.Count - kept.Count + 1))), fileName);
        }
    }

    // The files (as provided): each has ONE leading /* block comment */ (+ blank lines) before "USE [admindb];".
    [TestCase("core_object_predicates.sql", 6)]
    [TestCase("core_predicates.sql", 11)]
    [TestCase("database_defaults().sql", 6)]
    [TestCase("database_scoped_defaults().sql", 10)]
    [TestCase("engine_version().sql", 6)]
    [TestCase("execute_command.sql", 130)]                  // the header includes example code - with a GO.
    [TestCase("execute_per_database.sql", 99)]
    [TestCase("execute_per_database_errors.sql", 10)]
    [TestCase("execute_powershell.sql", 55)]
    [TestCase("extract_letters().sql", 13)]
    [TestCase("extract_numbers().sql", 13)]
    [TestCase("format_hex_html().sql", 53)]
    public void Header_Removal_Removes_The_Leading_Block_Comment(string fileName, int firstCodeLine)
    {
        var all = LinesFrom(_kept, fileName);
        var kept = LinesFrom(_removed, fileName);

        Assert.That(all, Is.Not.Empty, $"{fileName} was not included.");
        Assert.That(kept.First().OriginalContent, Is.EqualTo("USE [admindb];"));
        Assert.That(kept.First().LineNumber, Is.EqualTo(firstCodeLine));
        Assert.That(all.Count - kept.Count, Is.EqualTo(firstCodeLine - 1));

        Assert.That(all.First().OriginalContent, Is.EqualTo("/*"));
        Assert.That(all.Take(firstCodeLine - 1).Last(l => l.OriginalContent.Trim().Length > 0).OriginalContent.Trim(), Is.EqualTo("*/"));
    }

    [Test]
    public void Copyright_Tokens_Are_Replaced_And_Nothing_Else_Is()
    {
        int copyrights = 0, runtimeTokens = 0;

        foreach (var fileName in SampleFiles())
        {
            foreach (var line in LinesFrom(_tokens, fileName))
            {
                string where = $"{fileName} line {line.LineNumber}: {line.OriginalContent}";
                if (line.OriginalContent.Contains("{{##COPYRIGHT}}", StringComparison.Ordinal))
                {
                    copyrights++;
                    Assert.That(line.GetContent(), Is.EqualTo(line.OriginalContent.Replace("{{##COPYRIGHT}}", Copyright)), where);
                    continue;
                }

                // e.g., S4's (runtime) {SYSTEM}, {ALL}, {DEFAULT}, {CURRENT_DB} - with same-named (decoy) definitions.
                if (Regex.IsMatch(line.OriginalContent, @"\{(?:SYSTEM|ALL|DEFAULT|CURRENT_DB)\}"))
                    runtimeTokens++;

                Assert.That(line.TransformedContent, Is.Null, where);
            }
        }

        Assert.That(copyrights, Is.EqualTo(10));        // every sample file - except extract_letters() and extract_numbers().
        Assert.That(runtimeTokens, Is.GreaterThan(0));
    }
}
