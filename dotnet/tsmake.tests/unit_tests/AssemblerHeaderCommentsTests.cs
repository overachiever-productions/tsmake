namespace tsmake.tests.unit_tests;

// Real builds (in a throw-away temp directory): a build file that ##FILE-includes child.sql - with and without RemoveHeaderComments.
[TestFixture]
public class AssemblerHeaderCommentsTests
{
    private const string Child = "child.sql";
    private const string BuildFileName = "test.build.sql";

    // NOTE: code BEFORE the directives - otherwise they'd be the build file's header comments (and removed with it).
    private const string DefaultBuildFile = "SET NOCOUNT ON;\r\n-- ##OUTPUT: test_latest.sql\r\n-- ##FILE: child.sql";
    private const string HeaderOnlyBuildFile = "-- ##OUTPUT: test_latest.sql\r\n-- ##FILE: child.sql";

    private static string Text(params string[] lines) => string.Join("\r\n", lines);

    private static Assembler Build(string childContent, CommentRemovalDirectives removal, string buildContent = DefaultBuildFile,
        Dictionary<string, string>? otherFiles = null, LineEndingsType lineEndings = LineEndingsType.CrLf)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, Child), childContent);
            foreach (var (name, content) in otherFiles ?? new Dictionary<string, string>())
                File.WriteAllText(Path.Combine(temp, name), content);

            string buildFile = Path.Combine(temp, BuildFileName);
            File.WriteAllText(buildFile, buildContent);

            var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            options.SetDirectives(lineEndings, removal, TokenExclusionDirectives.None);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildFile);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());
            Assert.That(assembler.SyntaxErrors.Select(e => $"{e.LineNumber}: {e.Message}"), Is.Empty);
            return assembler;
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static List<ICodeLine> LinesFrom(Assembler assembler, string fileName) =>
        assembler.ArtifactLines.Where(l => Path.GetFileName(l.FileDetail.FilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase)).ToList();

    // Every artifact line EXCEPT the build file's own (i.e., the included files - in order).
    private static IEnumerable<string> IncludedLines(Assembler assembler) =>
        assembler.ArtifactLines.Where(l => !Path.GetFileName(l.FileDetail.FilePath).Equals(BuildFileName, StringComparison.OrdinalIgnoreCase)).Select(l => l.OriginalContent);

    private const string FileWithHeader =
        "-- this is part of header - as we haven't hit non-comment content yet. \r\n" +
        "/* this is part of the \r\n" +
        "comment \r\n" +
        "too */\r\n" +
        "-- but everything after this line is non-HEADER comment because next line = T-SQL\r\n" +
        "USE [myDatabase];\r\n" +
        "GO \r\n" +
        "\r\n" +
        "-- this is a comment - but NOT a header comment.";

    [Test]
    public void It_Removes_Header_Comments_From_Included_Files()
    {
        var lines = LinesFrom(Build(FileWithHeader, CommentRemovalDirectives.RemoveHeaderComments), Child);

        Assert.That(lines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "USE [myDatabase];", "GO ", "", "-- this is a comment - but NOT a header comment." }));
        Assert.That(lines.Select(l => l.LineNumber), Is.EqualTo(new[] { 6, 7, 8, 9 }));      // ORIGINAL line numbers are kept (for error reporting).
    }

    [TestCase(CommentRemovalDirectives.RemoveNone)]
    [TestCase(CommentRemovalDirectives.RemoveDocComments)]
    public void It_Keeps_Header_Comments_Unless_Told_To_Remove_Them(CommentRemovalDirectives removal)
    {
        var lines = LinesFrom(Build(FileWithHeader, removal), Child);

        Assert.That(string.Join("\r\n", lines.Select(l => l.OriginalContent)), Is.EqualTo(FileWithHeader));
        Assert.That(lines.Select(l => l.LineNumber), Is.EqualTo(Enumerable.Range(1, 9)));
    }

    [TestCase("\n", LineEndingsType.CrLf)]
    [TestCase("\r", LineEndingsType.CrLf)]
    [TestCase("\r\n", LineEndingsType.Lf)]
    [TestCase("\r\n", LineEndingsType.Cr)]
    public void It_Removes_Header_Comments_Regardless_Of_Line_Endings(string lineEnding, LineEndingsType target)
    {
        var lines = LinesFrom(Build(FileWithHeader.Replace("\r\n", lineEnding), CommentRemovalDirectives.RemoveHeaderComments, lineEndings: target), Child);

        Assert.That(lines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "USE [myDatabase];", "GO ", "", "-- this is a comment - but NOT a header comment." }));
        Assert.That(lines.Select(l => l.LineNumber), Is.EqualTo(new[] { 6, 7, 8, 9 }));
    }

    [TestCase("/* block comment here */USE [admindb];\r\nGO ", 1)]
    [TestCase("/* a block-comment that spans\r\n   lines */USE [admindb];\r\nGO ", 2)]
    [TestCase("-- header\r\n/* yet another block comment */  USE [admindb];\r\nGO ", 2)]
    public void It_Removes_Only_The_Part_Of_The_Code_Line_Before_The_Code(string content, int codeLineNumber)
    {
        var lines = LinesFrom(Build(content, CommentRemovalDirectives.RemoveHeaderComments), Child);

        Assert.That(lines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "USE [admindb];", "GO " }));
        Assert.That(lines[0].LineNumber, Is.EqualTo(codeLineNumber));
    }

    [Test]
    public void It_Keeps_The_Indentation_Of_The_Code_Line()
    {
        var lines = LinesFrom(Build("      -- this is a comment (preceded by white-space)\r\n     SELECT N'code starts here';", CommentRemovalDirectives.RemoveHeaderComments), Child);

        Assert.That(lines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "     SELECT N'code starts here';" }));
        Assert.That(lines[0].LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void It_Does_Not_Alter_Files_Without_Header_Comments()
    {
        string content = "\r\nUSE [master];  -- this isn't a header comment. \r\nGO";

        var lines = LinesFrom(Build(content, CommentRemovalDirectives.RemoveHeaderComments), Child);

        Assert.That(lines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "", "USE [master];  -- this isn't a header comment. ", "GO" }));
    }

    [TestCase("-- 6.0\r\n-- 6.1\r\n/* 6.2 */")]
    [TestCase("-- 6.0\r\n-- 6.1\r\n/* 6.2 */\r\n\r\n")]
    public void A_File_Of_Only_Comments_Contributes_Nothing(string content)
    {
        var assembler = Build(content, CommentRemovalDirectives.RemoveHeaderComments);

        Assert.That(LinesFrom(assembler, Child), Is.Empty);
    }

    // AS DESIGNED (for now): directives are -- comments - so any that come before a file's first code are removed with its header.
    [Test]
    public void Directives_In_The_Header_Are_Removed_With_It()
    {
        string child = Text("/* child header */", "-- ##FILE: grandchild.sql", "-- also header", "SELECT 'child';");
        var others = new Dictionary<string, string> { ["grandchild.sql"] = "SELECT 'grandchild';" };

        var removed = Build(child, CommentRemovalDirectives.RemoveHeaderComments, otherFiles: others);
        var kept = Build(child, CommentRemovalDirectives.RemoveNone, otherFiles: others);

        Assert.That(IncludedLines(removed), Is.EqualTo(new[] { "SELECT 'child';" }));
        Assert.That(IncludedLines(kept), Is.EqualTo(new[] { "/* child header */", "SELECT 'grandchild';", "-- also header", "SELECT 'child';" }));
    }

    [Test]
    public void Directives_After_The_First_Code_Are_Still_Processed()
    {
        string child = Text("/* child header */", "SELECT 'child';", "-- ##FILE: grandchild.sql", "-- not header: after code");
        var others = new Dictionary<string, string> { ["grandchild.sql"] = Text("-- grandchild header", "SELECT 'grandchild';") };

        var assembler = Build(child, CommentRemovalDirectives.RemoveHeaderComments, otherFiles: others);

        Assert.That(IncludedLines(assembler), Is.EqualTo(new[] { "SELECT 'child';", "SELECT 'grandchild';", "-- not header: after code" }));
    }

    [Test]
    public void It_Removes_The_Header_Comments_Of_The_Build_File_Too()
    {
        string buildFile = Text("/* build-file header */", "-- ##OUTPUT: test_latest.sql", "PRINT 'build file';", "-- ##FILE: child.sql");

        var assembler = Build("-- child header\r\nPRINT 'child';", CommentRemovalDirectives.RemoveHeaderComments, buildFile);

        Assert.That(assembler.ArtifactLines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "PRINT 'build file';", "PRINT 'child';" }));
    }

    [Test]
    public void The_Build_Files_Own_Includes_Are_Removed_If_They_Are_In_Its_Header()
    {
        // AS DESIGNED (for now) - see Global.HeaderCommentsRegex. NOTE: ##OUTPUT/##ROOT still work: they're read in a separate pass.
        var assembler = Build("PRINT 'child';", CommentRemovalDirectives.RemoveHeaderComments, HeaderOnlyBuildFile);

        Assert.That(assembler.ArtifactLines, Is.Empty);
        Assert.That(LinesFrom(Build("PRINT 'child';", CommentRemovalDirectives.RemoveNone, HeaderOnlyBuildFile), Child).Select(l => l.OriginalContent),
            Is.EqualTo(new[] { "PRINT 'child';" }));
    }
}
