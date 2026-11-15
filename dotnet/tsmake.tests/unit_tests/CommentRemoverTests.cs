using System.Text;

namespace tsmake.tests.unit_tests;

[TestFixture]
public class CommentRemoverTests
{
    private const CommentRemovalDirectives Eol = CommentRemovalDirectives.RemoveLineEndComments;
    private const CommentRemovalDirectives Block = CommentRemovalDirectives.RemoveBlockComments;
    private const CommentRemovalDirectives All = CommentRemovalDirectives.RemoveAllComments;

    private static string Remove(string body, CommentRemovalDirectives directives) => CommentRemover.RemoveComments(body, directives);

    #region Directives
    [TestCase(CommentRemovalDirectives.NotSet)]
    [TestCase(CommentRemovalDirectives.RemoveNone)]
    [TestCase(CommentRemovalDirectives.RemoveHeaderComments)]       // handled per FILE - earlier.
    [TestCase(CommentRemovalDirectives.RemoveDocComments)]          // PLACE-HOLDER: nothing is removed (yet).
    public void Directives_Without_Eol_Or_Block_Leave_The_Body_Alone(CommentRemovalDirectives directives)
    {
        string body = "/* block */\r\nSELECT 1; -- eol";
        Assert.That(Remove(body, directives), Is.SameAs(body));
    }

    [Test]
    public void RemoveAll_Removes_Both_Kinds()
    {
        Assert.That(Remove("SELECT 1; /* block */ -- eol\r\nSELECT 2;", All), Is.EqualTo("SELECT 1;\r\nSELECT 2;"));
    }

    [Test]
    public void Each_Kind_Is_Removed_On_Its_Own()
    {
        string body = "SELECT 1; -- eol\r\nSELECT /* block */ 2;";
        Assert.That(Remove(body, Eol), Is.EqualTo("SELECT 1;\r\nSELECT /* block */ 2;"));
        Assert.That(Remove(body, Block), Is.EqualTo("SELECT 1; -- eol\r\nSELECT 2;"));
    }

    [Test]
    public void A_Body_Without_Comments_Is_Returned_As_Is()
    {
        string body = "SELECT 1 - 2 / 3 * 4, '-- string', [/* identifier */];";
        Assert.That(Remove(body, All), Is.SameAs(body));
    }
    #endregion

    #region -- comments
    [TestCase("SELECT 1; -- note", "SELECT 1;")]
    [TestCase("SELECT 1;-- note", "SELECT 1;")]
    [TestCase("SELECT 1; \t -- note", "SELECT 1;")]
    [TestCase("SELECT 1--1", "SELECT 1")]
    [TestCase("-- note\r\nSELECT 1;", "SELECT 1;")]
    [TestCase("SELECT 1;\r\n    -- note\r\nSELECT 2;", "SELECT 1;\r\nSELECT 2;")]
    [TestCase("SELECT 1;\r\n-- one\r\n-- two\r\nSELECT 2;", "SELECT 1;\r\nSELECT 2;")]
    [TestCase("-- don't\r\nSELECT 'x';", "SELECT 'x';")]                                // a tick in a comment doesn't start a string.
    [TestCase("-- see /* here\r\nSELECT 1;", "SELECT 1;")]                              // nor does a /*.
    public void Eol_Comments_Are_Removed(string body, string expected)
    {
        Assert.That(Remove(body, Eol), Is.EqualTo(expected));
    }

    [TestCase("SELECT '-- not a comment';")]
    [TestCase("SELECT N'it''s -- still a string';")]
    [TestCase("SELECT 'line one\r\n-- still a string';")]
    [TestCase("SELECT [a--b], [x]]--y] FROM t;")]
    [TestCase("SELECT \"a--b\", \"x\"\"--y\" FROM t;")]
    [TestCase("/* -- in a block comment */ SELECT 1;")]
    [TestCase("/*\r\n-- in a block comment\r\n*/")]
    [TestCase("SELECT 'never closed -- not a comment")]
    public void Eol_Markers_That_Are_Not_In_Code_Are_Ignored(string body)
    {
        Assert.That(Remove(body, Eol), Is.EqualTo(body));
    }
    #endregion

    #region /* block comments */
    [TestCase("SELECT /* note */ 1;", "SELECT 1;")]
    [TestCase("SELECT/*note*/1;", "SELECT 1;")]                                         // never merge tokens.
    [TestCase("SELECT/* note */ 1;", "SELECT 1;")]
    [TestCase("SELECT /* note */1;", "SELECT 1;")]
    [TestCase("SELECT 1; /* note */", "SELECT 1;")]
    [TestCase("/* note */ SELECT 1;", "SELECT 1;")]
    [TestCase("    /* note */ SELECT 1;", "    SELECT 1;")]                            // indentation is kept.
    [TestCase("/* note */\r\nSELECT 1;", "SELECT 1;")]
    [TestCase("SELECT 1;\r\n/* line one\r\n   line two */\r\nSELECT 2;", "SELECT 1;\r\nSELECT 2;")]
    [TestCase("SELECT a, /* line one\r\n line two */ b", "SELECT a, b")]
    [TestCase("/* outer /* inner */ still outer */SELECT 1;", "SELECT 1;")]            // nested.
    [TestCase("/* a /* b /* c */ */ */ SELECT 1; /* d */", "SELECT 1;")]
    [TestCase("SELECT 1/*a*//*b*/2", "SELECT 1 2")]
    [TestCase("/*a*//*b*/\r\nSELECT 1;", "SELECT 1;")]
    [TestCase("SELECT t.*/* all columns */ FROM t;", "SELECT t.* FROM t;")]
    [TestCase("/* it's */ SELECT 'x';", "SELECT 'x';")]                                // a tick in a comment doesn't start a string.
    [TestCase("/* [ */ SELECT 1;", "SELECT 1;")]
    [TestCase("/* ** / */ SELECT 1;", "SELECT 1;")]
    public void Block_Comments_Are_Removed(string body, string expected)
    {
        Assert.That(Remove(body, Block), Is.EqualTo(expected));
    }

    [TestCase("SELECT '/* not a comment */';")]
    [TestCase("SELECT '/*', '*/';")]
    [TestCase("SELECT N'it''s /* still */ a string';")]
    [TestCase("SELECT [/* identifier */], \"/* identifier */\";")]
    [TestCase("-- see /* here\r\nSELECT 1; -- and */ there")]                        // /* and */ in -- comments.
    [TestCase("SELECT 1; -- */")]
    [TestCase("SELECT 2 */ 1;")]                                                        // a stray */ is not a comment.
    [TestCase("SELECT 1; /* never closed\r\nSELECT 2;")]                                // unterminated: left alone (already a syntax error).
    [TestCase("SELECT 1; /* outer /* inner */ never closed")]
    public void Block_Markers_That_Are_Not_Comments_Are_Ignored(string body)
    {
        Assert.That(Remove(body, Block), Is.EqualTo(body));
    }

    [Test]
    public void Nothing_After_An_Unterminated_Comment_Is_Treated_As_Code()
    {
        string body = "SELECT 1; -- removed\r\n/* never closed\r\n-- inside the comment\r\n/* inside */";
        Assert.That(Remove(body, All), Is.EqualTo("SELECT 1;\r\n/* never closed\r\n-- inside the comment\r\n/* inside */"));
    }
    #endregion

    #region Layout
    [TestCase("SELECT 1; /* a */ -- b\r\n/* c */ -- d\r\nSELECT 2;", "SELECT 1;\r\nSELECT 2;")]
    [TestCase("/* a */ -- b", "")]
    [TestCase("-- only comments\r\n/* here */\r\n", "")]
    [TestCase("SELECT 1;\r\n-- the last line", "SELECT 1;")]                            // the last line's comment takes the line break BEFORE it.
    [TestCase("SELECT 1;\r\n-- the last line\r\n", "SELECT 1;\r\n")]                    // ... unless the body ends with a line break.
    [TestCase("SELECT 1;\r\n\r\n-- note\r\n\r\nSELECT 2;", "SELECT 1;\r\n\r\n\r\nSELECT 2;")]   // blank lines (that were ALWAYS blank) are kept.
    [TestCase("SELECT 1;   \r\n-- note", "SELECT 1;   ")]                              // trailing whitespace on OTHER lines is kept.
    public void Comment_Only_Lines_And_Surrounding_Whitespace_Are_Removed(string body, string expected)
    {
        Assert.That(Remove(body, All), Is.EqualTo(expected));
    }

    [TestCase("\r\n")]
    [TestCase("\n")]
    [TestCase("\r")]
    public void Every_Line_Ending_Works(string lineEnding)
    {
        string body = string.Join(lineEnding, "SELECT 1; -- a", "  /* b", "  c */", "SELECT /* d */ 2;", "-- e");
        Assert.That(Remove(body, All), Is.EqualTo(string.Join(lineEnding, "SELECT 1;", "SELECT 2;")));
    }
    #endregion

    #region vs. TSqlScanner (an independent T-SQL lexer)
    // What's left of the text - per the TSqlScanner - with every comment swapped for a single space (i.e., how SQL Server sees it).
    private static string WithoutComments(string text)
    {
        var scanner = new TSqlScanner();
        var output = new StringBuilder();
        foreach (string line in Regex.Split(text, @"\r\n|\r|\n"))
        {
            SqlRegion region = scanner.Region;
            var changes = new List<(int Index, SqlRegion Region)>();
            scanner.ScanLine(line, (index, newRegion) => changes.Add((index, newRegion)));

            int change = 0;
            bool inComment = false;
            for (int i = 0; i < line.Length; i++)
            {
                while (change < changes.Count && changes[change].Index <= i)
                    region = changes[change++].Region;

                bool comment = region is SqlRegion.EolComment or SqlRegion.BlockComment;
                if (!comment)
                    output.Append(line[i]);
                else if (!inComment)
                    output.Append(' ');
                inComment = comment;
            }
            output.Append('\n');
        }
        return output.ToString();
    }

    private static bool HasComments(string text)
    {
        var scanner = new TSqlScanner();
        bool found = false;
        foreach (string line in Regex.Split(text, @"\r\n|\r|\n"))
            scanner.ScanLine(line, (_, region) => found |= region is SqlRegion.EolComment or SqlRegion.BlockComment);
        return found;
    }

    private static bool EndsInCode(string text)
    {
        var scanner = new TSqlScanner();
        foreach (string line in Regex.Split(text, @"\r\n|\r|\n"))
            scanner.ScanLine(line);
        return scanner.Region == SqlRegion.Code;
    }

    private static string[] Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    // Same words (in the same order) as the scanner - i.e., ONLY comments were removed, and no two tokens were merged. Nothing left to remove.
    private static void AssertMatchesScanner(string body, string because)
    {
        string removed = Remove(body, All);
        Assert.That(Words(removed), Is.EqualTo(Words(WithoutComments(body))), because);
        Assert.That(HasComments(removed), Is.False, because);
        Assert.That(Remove(removed, All), Is.SameAs(removed), because);
    }

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

    [Test]
    public void Real_Files_Match_The_Scanner()
    {
        var files = Directory.GetFiles(FindSamplesFolder(), "*.sql");
        Assert.That(files, Is.Not.Empty);

        foreach (string file in files)
            AssertMatchesScanner(File.ReadAllText(file), Path.GetFileName(file));
    }

    [Test]
    public void Random_Fragments_Match_The_Scanner()
    {
        string[] fragments =
        [
            "SELECT", "1", " ", "  ", "\t", "\r\n", "\n", ",", "(", ")", "-", "/", "*", "'", "[", "]", "\"", "--", "/*", "*/",
            "'a''b'", "'-- s'", "'/* s */'", "[i--d]", "[i/*d]", "\"q--\"", "-- c", "-- it's", "-- /* c", "/* b */", "/* it's */",
            "/* -- b */", "/* n /* m */ */", "N'x'", "t.*", "*/*"
        ];

        var random = new Random(20261114);      // fixed seed: the same cases every run.
        int tested = 0;
        for (int i = 0; i < 20000; i++)
        {
            var body = new StringBuilder();
            int count = random.Next(1, 16);
            for (int f = 0; f < count; f++)
                body.Append(fragments[random.Next(fragments.Length)]);

            string text = body.ToString();
            if (!EndsInCode(text))              // unterminated strings/comments: SQL Server (and the Normalizer) call these errors.
                continue;

            AssertMatchesScanner(text, $"[{text.Replace("\r", "\\r").Replace("\n", "\\n")}]");
            tested++;
        }

        Assert.That(tested, Is.GreaterThan(5000));
    }
    #endregion

    #region Assembler.ArtifactBody
    private static Assembler Build(string childContent, CommentRemovalDirectives directives)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "child.sql"), childContent);
            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, "SET NOCOUNT ON; -- build file\r\n-- ##OUTPUT: test_latest.sql\r\n-- ##FILE: child.sql");

            var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            options.SetDirectives(LineEndingsType.CrLf, directives, TokenExclusionDirectives.None);

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

    private const string Child = "/* child header */\r\nSELECT N'-- kept', [x/*y*/]; -- gone\r\n/* gone\r\n   too */\r\nPRINT 'done'; /* gone */";

    [Test]
    public void The_Assembler_Removes_Comments_From_The_Artifact_Body()
    {
        var assembler = Build(Child, Eol | Block);

        Assert.That(assembler.ArtifactBody, Is.EqualTo("SET NOCOUNT ON;\r\nSELECT N'-- kept', [x/*y*/];\r\nPRINT 'done';"));
        Assert.That(assembler.ArtifactLines.Select(l => l.GetContent()), Does.Contain("PRINT 'done'; /* gone */"));      // lines are untouched.
    }

    [TestCase(CommentRemovalDirectives.RemoveNone)]
    [TestCase(CommentRemovalDirectives.RemoveDocComments)]
    public void The_Assembler_Keeps_Comments_Unless_Asked(CommentRemovalDirectives directives)
    {
        var assembler = Build(Child, directives);

        Assert.That(assembler.ArtifactBody, Is.EqualTo("SET NOCOUNT ON; -- build file\r\n" + Child));
    }
    #endregion
}
