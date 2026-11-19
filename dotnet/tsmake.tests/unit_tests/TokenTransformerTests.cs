namespace tsmake.tests.unit_tests;

[TestFixture]
public class TokenTransformerTests
{
    private const string Summary = "Lots of fixin' and stuff";

    private static readonly (string Name, string Value)[] DefaultTokens =
    [
        ("COPYRIGHT", "(c) 2026 OverAchiever Productions"),
        ("VERSION", "7.1.2"),
        ("SUMMARY", Summary),
        ("PROJECT-SITE", "https://www.totalsql.com"),
        ("V", "1"),
        ("EMPTY", "")
    ];

    private static List<ICodeLine> Lines(params string[] lines) =>
        lines.Select((line, i) => (ICodeLine)new CodeLine(line, i + 1, 0, 0, new FileDetail("file-name.sql", 0, 0))).ToList();

    private static List<ICodeLine> TransformLines(List<ICodeLine> lines, TokenExclusionDirectives exclusions, params (string Name, string Value)[] tokens)
    {
        var registry = TokenDefinitionRegistry.CreateRegistry(tokens.Select(t => new TokenDefinition(t.Name, t.Value)));
        new TokenTransformer(exclusions, registry).TransformCodeLines(lines, new List<ISyntaxError>());
        return lines;
    }

    private static string[] Transform(TokenExclusionDirectives exclusions, params string[] lines) =>
        TransformLines(Lines(lines), exclusions, DefaultTokens).Select(l => l.GetContent()).ToArray();

    private static string Transform(string line, TokenExclusionDirectives exclusions = TokenExclusionDirectives.None) => Transform(exclusions, line)[0];

    #region Token syntax
    [TestCase("{{##COPYRIGHT}}", "(c) 2026 OverAchiever Productions")]
    [TestCase("{{##VERSION}}", "7.1.2")]
    [TestCase("{{##PROJECT-SITE}}", "https://www.totalsql.com")]
    [TestCase("-- {{##COPYRIGHT}}", "-- (c) 2026 OverAchiever Productions")]
    [TestCase("SELECT '{{##VERSION}}' [version];", "SELECT '7.1.2' [version];")]
    [TestCase("{{##VERSION}} {{##PROJECT-SITE}} {{##VERSION}}", "7.1.2 https://www.totalsql.com 7.1.2")]
    [TestCase("x{{##V}}y{{##V}}z", "x1y1z")]
    public void It_Replaces_Defined_Tokens(string line, string expected)
    {
        Assert.That(Transform(line), Is.EqualTo(expected));
    }

    [TestCase("{{##copyright}}")]           // not upper-case.
    [TestCase("{{##CopyRIGHT}}")]           // not (entirely) upper-case.
    [TestCase("{{COPYRIGHT}}")]             // no ## preamble.
    [TestCase("{{#COPYRIGHT}}")]
    [TestCase("##{{COPYRIGHT}}")]           // the OLD syntax.
    [TestCase("{##COPYRIGHT}")]
    [TestCase("{{ ##COPYRIGHT}}")]          // whitespace before the preamble ...
    [TestCase("{{## COPYRIGHT}}")]          // ... or after it ...
    [TestCase("{{##COPYRIGHT }}")]          // ... or anywhere.
    [TestCase("{{##COPY RIGHT}}")]
    [TestCase("{{##COPY_RIGHT}}")]          // no underscores.
    [TestCase("{{##COPY--RIGHT}}")]         // single dashes only ...
    [TestCase("{{##-COPYRIGHT}}")]          // ... BETWEEN letters/numbers.
    [TestCase("{{##COPYRIGHT-}}")]
    [TestCase("{{##}}")]
    public void It_Only_Matches_Valid_Token_Syntax(string text)
    {
        Assert.That(Global.TokenRegex.IsMatch(text), Is.False);

        var lines = TransformLines(Lines(text), TokenExclusionDirectives.None, ("COPYRIGHT", "replaced!"), ("COPY-RIGHT", "replaced!"));
        Assert.That(lines[0].GetContent(), Is.EqualTo(text));
        Assert.That(lines[0].TransformedContent, Is.Null);
    }

    [Test]
    public void S4_Runtime_Tokens_Are_Never_Replaced()
    {
        // S4's own {TOKENS} are resolved at RUNTIME (by S4) - tsmake must leave them alone, even with similarly named definitions.
        string[] s4 =
        [
            "EXEC dbo.[execute_per_database] @Databases = N'{SYSTEM},{CURRENT_DB}', @Command = N'{ALL}';",
            "IF @Outcome = N'{COMMAND_SUCCESS}' OR @Mode = N'{DEFAULT}' SELECT N'{ROWS_AFFECTED}';",
            "-- {copyright}",
            "-- v7.0+ - Conversion of [tokens] to {tokens}.",
            "SELECT N'{TOKENS}', '{ALL}';"
        ];

        var lines = TransformLines(Lines(s4), TokenExclusionDirectives.None,
            ("SYSTEM", "x"), ("CURRENT-DB", "x"), ("ALL", "x"), ("DEFAULT", "x"), ("COPYRIGHT", "x"), ("TOKENS", "x"));

        Assert.That(lines.Select(l => l.GetContent()), Is.EqualTo(s4));
        Assert.That(lines.Select(l => l.TransformedContent), Is.All.Null);
    }
    #endregion

    #region Undefined tokens, values
    [Test]
    public void Undefined_Tokens_Are_Left_As_They_Are()
    {
        var lines = TransformLines(Lines("PRINT N'{{##UNDEFINED}}';", "{{##VERSION}} and {{##UNDEFINED}} and {{##VERSION}}"), TokenExclusionDirectives.None, DefaultTokens);

        Assert.That(lines[0].GetContent(), Is.EqualTo("PRINT N'{{##UNDEFINED}}';"));
        Assert.That(lines[0].TransformedContent, Is.Null);
        Assert.That(lines[1].GetContent(), Is.EqualTo("7.1.2 and {{##UNDEFINED}} and 7.1.2"));
    }

    [Test]
    public void Nothing_Changes_Without_Any_Token_Definitions()
    {
        var lines = TransformLines(Lines("-- {{##COPYRIGHT}}", "SELECT 1;"), TokenExclusionDirectives.None);

        Assert.That(lines.Select(l => l.TransformedContent), Is.All.Null);
        Assert.That(lines.Select(l => l.GetContent()), Is.EqualTo(new[] { "-- {{##COPYRIGHT}}", "SELECT 1;" }));
    }

    [Test]
    public void Lines_Without_Replacements_Are_Not_Transformed()
    {
        var lines = TransformLines(Lines("SELECT 1;", "-- no tokens here", "{{##UNDEFINED}}"), TokenExclusionDirectives.None, DefaultTokens);

        Assert.That(lines.Select(l => l.TransformedContent), Is.All.Null);
    }

    [Test]
    public void Empty_Values_Replace_The_Token_With_Nothing()
    {
        var lines = TransformLines(Lines("PRINT N'{{##EMPTY}}';", "{{##EMPTY}}"), TokenExclusionDirectives.None, DefaultTokens);

        Assert.That(lines[0].GetContent(), Is.EqualTo("PRINT N'';"));

        // A line that becomes EMPTY is still transformed (i.e., it doesn't fall back to its original content).
        Assert.That(lines[1].TransformedContent, Is.EqualTo(string.Empty));
        Assert.That(lines[1].GetContent(), Is.EqualTo(string.Empty));
    }

    [TestCase("$1 $& $$ $_ ${name} $`")]
    [TestCase("C:\\Program Files\\$Recycle.Bin")]
    public void Values_Are_Inserted_Literally(string value)
    {
        var lines = TransformLines(Lines("-- {{##X}}"), TokenExclusionDirectives.None, ("X", value));

        Assert.That(lines[0].GetContent(), Is.EqualTo("-- " + value));
    }

    [Test]
    public void Values_Containing_Tokens_Are_Not_Expanded_Again()
    {
        var lines = TransformLines(Lines("-- {{##A}}"), TokenExclusionDirectives.None, ("A", "{{##B}}"), ("B", "b"));

        Assert.That(lines[0].GetContent(), Is.EqualTo("-- {{##B}}"));
    }
    #endregion

    #region Escaping (values INSIDE strings and identifiers)
    [TestCase("PRINT N'{{##SUMMARY}}';", "PRINT N'Lots of fixin'' and stuff';")]
    [TestCase("SELECT '{{##SUMMARY}}' + N' / {{##SUMMARY}}';", "SELECT 'Lots of fixin'' and stuff' + N' / Lots of fixin'' and stuff';")]
    [TestCase("-- {{##SUMMARY}}", "-- Lots of fixin' and stuff")]                  // NOT in a string: as-is.
    [TestCase("/* {{##SUMMARY}} */", "/* Lots of fixin' and stuff */")]
    [TestCase("SELECT 1 AS [{{##SUMMARY}}];", "SELECT 1 AS [Lots of fixin' and stuff];")]
    public void Apostrophes_Are_Doubled_Inside_Strings(string line, string expected)
    {
        Assert.That(Transform(line), Is.EqualTo(expected));
    }

    [TestCase("SELECT 1 AS [{{##X}}];", "SELECT 1 AS [a]]b\"c'd];")]
    [TestCase("SELECT 1 AS \"{{##X}}\";", "SELECT 1 AS \"a]b\"\"c'd\";")]
    [TestCase("SELECT '{{##X}}';", "SELECT 'a]b\"c''d';")]
    [TestCase("SELECT {{##X}};", "SELECT a]b\"c'd;")]
    public void A_Regions_Own_Closing_Delimiter_Is_Doubled(string line, string expected)
    {
        var lines = TransformLines(Lines(line), TokenExclusionDirectives.None, ("X", "a]b\"c'd"));

        Assert.That(lines[0].GetContent(), Is.EqualTo(expected));
    }

    [Test]
    public void Multi_Line_Strings_Are_Tracked_Across_Lines()
    {
        var result = Transform(TokenExclusionDirectives.None, "PRINT N'first line", "{{##SUMMARY}}';", "-- {{##SUMMARY}}");

        Assert.That(result, Is.EqualTo(new[] { "PRINT N'first line", "Lots of fixin'' and stuff';", "-- Lots of fixin' and stuff" }));
    }
    #endregion

    #region Exclusions
    private static readonly string[] Regions =
    [
        "SELECT {{##V}} AS [{{##V}}], '{{##V}}' AS \"{{##V}}\"; -- {{##V}}",
        "/* {{##V}} */ {{##V}}",
        "/* multi-line {{##V}}",
        "{{##V}} */ {{##V}}"
    ];

    [TestCase(TokenExclusionDirectives.None,
        "SELECT 1 AS [1], '1' AS \"1\"; -- 1", "/* 1 */ 1", "/* multi-line 1", "1 */ 1")]
    [TestCase(TokenExclusionDirectives.ExcludeStrings,
        "SELECT 1 AS [1], '{{##V}}' AS \"1\"; -- 1", "/* 1 */ 1", "/* multi-line 1", "1 */ 1")]
    [TestCase(TokenExclusionDirectives.ExcludeEolComments,
        "SELECT 1 AS [1], '1' AS \"1\"; -- {{##V}}", "/* 1 */ 1", "/* multi-line 1", "1 */ 1")]
    [TestCase(TokenExclusionDirectives.ExcludeBlockComments,
        "SELECT 1 AS [1], '1' AS \"1\"; -- 1", "/* {{##V}} */ 1", "/* multi-line {{##V}}", "{{##V}} */ 1")]
    [TestCase(TokenExclusionDirectives.ExcludeStrings | TokenExclusionDirectives.ExcludeBlockComments,
        "SELECT 1 AS [1], '{{##V}}' AS \"1\"; -- 1", "/* {{##V}} */ 1", "/* multi-line {{##V}}", "{{##V}} */ 1")]
    [TestCase(TokenExclusionDirectives.ExcludeStrings | TokenExclusionDirectives.ExcludeEolComments | TokenExclusionDirectives.ExcludeBlockComments,
        "SELECT 1 AS [1], '{{##V}}' AS \"1\"; -- {{##V}}", "/* {{##V}} */ 1", "/* multi-line {{##V}}", "{{##V}} */ 1")]
    public void Exclusions_Skip_Tokens_In_Their_Regions_Only(TokenExclusionDirectives exclusions, string line1, string line2, string line3, string line4)
    {
        Assert.That(Transform(exclusions, Regions), Is.EqualTo(new[] { line1, line2, line3, line4 }));
    }

    [TestCase("PRINT '-- {{##V}}';", TokenExclusionDirectives.ExcludeEolComments, "PRINT '-- 1';")]               // -- in a string: NOT a comment.
    [TestCase("PRINT '/* {{##V}}'; {{##V}}", TokenExclusionDirectives.ExcludeBlockComments, "PRINT '/* 1'; 1")]   // /* in a string: NOT a comment.
    [TestCase("SELECT [--] = {{##V}};", TokenExclusionDirectives.ExcludeEolComments, "SELECT [--] = 1;")]
    [TestCase("SELECT 1; -- it's {{##V}}", TokenExclusionDirectives.ExcludeStrings, "SELECT 1; -- it's 1")]       // ' in a comment: NOT a string.
    [TestCase("{{##V}} -- {{##V}}", TokenExclusionDirectives.ExcludeEolComments, "1 -- {{##V}}")]                 // only AFTER the --.
    [TestCase("/* a /* b */ {{##V}} */ {{##V}}", TokenExclusionDirectives.ExcludeBlockComments, "/* a /* b */ {{##V}} */ 1")]    // nested.
    public void Regions_Follow_T_SQL_Rules(string line, TokenExclusionDirectives exclusions, string expected)
    {
        Assert.That(Transform(line, exclusions), Is.EqualTo(expected));
    }

    [Test]
    public void A_Tick_In_A_Comment_Does_Not_Open_A_String_On_Later_Lines()
    {
        var result = Transform(TokenExclusionDirectives.ExcludeStrings, "-- don't", "SELECT {{##V}}, '{{##V}}';");

        Assert.That(result[1], Is.EqualTo("SELECT 1, '{{##V}}';"));
    }

    [Test]
    public void Regions_Are_Tracked_Across_The_Whole_Artifact()
    {
        // ArtifactLines are in OUTPUT order: here, an included file's lines land INSIDE the parent's block comment.
        var parent = new FileDetail("parent.sql", 0, 1);
        var child = new FileDetail("child.sql", 2, 2);
        var lines = new List<ICodeLine>
        {
            new CodeLine("SELECT 1; /* start", 1, 0, 0, parent),
            new CodeLine("{{##V}}", 1, 0, 0, child),
            new CodeLine("end */ {{##V}}", 3, 0, 0, parent)
        };

        TransformLines(lines, TokenExclusionDirectives.ExcludeBlockComments, ("V", "1"));

        Assert.That(lines.Select(l => l.GetContent()), Is.EqualTo(new[] { "SELECT 1; /* start", "{{##V}}", "end */ 1" }));
    }
    #endregion

    #region Assembler (end-to-end)
    private static Assembler Build(string buildFile, Dictionary<string, string> files, TokenExclusionDirectives exclusions, params (string Name, string Value)[] tokens)
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
            options.SetDirectives(LineEndingsType.CrLf, CommentRemovalDirectives.RemoveNone, exclusions);

            var result = new BuildResult();
            var assembler = new Assembler(new OpsFactory(new tsmake.FileSystem(temp), options), options, result);
            assembler.Assemble(buildPath);

            Assert.That(result.Exception, Is.Null, result.Exception?.ToString());
            return assembler;
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public void The_Assembler_Replaces_Tokens_In_Every_Included_File()
    {
        var files = new Dictionary<string, string>
        {
            ["child.sql"] = "PRINT N'{{##SUMMARY}}'; -- {{##VERSION}}\r\nSELECT N'{SYSTEM}', '{{##UNDEFINED}}';",
        };

        var assembler = Build("SET NOCOUNT ON; -- v{{##VERSION}}\r\n-- ##OUTPUT: test_latest.sql\r\n-- ##FILE: child.sql", files, TokenExclusionDirectives.ExcludeEolComments,
            ("SUMMARY", Summary), ("VERSION", "7.1.2"), ("SYSTEM", "x"));

        Assert.That(assembler.SyntaxErrors.Select(e => e.Message), Is.Empty);
        Assert.That(assembler.ArtifactLines.Select(l => l.GetContent()), Is.EqualTo(new[] {
            "SET NOCOUNT ON; -- v{{##VERSION}}",
            "PRINT N'Lots of fixin'' and stuff'; -- {{##VERSION}}",
            "SELECT N'{SYSTEM}', '{{##UNDEFINED}}';" }));
    }

    [Test]
    public void Tokens_Within_Directives_Are_Syntax_Errors()
    {
        string buildFile = string.Join("\r\n",
            "SET NOCOUNT ON;",
            "-- ##OUTPUT: test_latest.sql",
            "-- ##FILE: {{##VERSION}}.sql",                      // line 3: an error.
            "-- ##FILE: child.sql ##:: built for {{##VERSION}}",  // a ##:: comment: fine.
            "-- ##COMMENT: version {{##VERSION}}",               // a comment directive: fine.
            "-- ##:: version {{##VERSION}}");                    // ditto.

        var assembler = Build(buildFile, new Dictionary<string, string> { ["child.sql"] = "SELECT 1;" }, TokenExclusionDirectives.None, ("VERSION", "7.1.2"));

        var tokenErrors = assembler.SyntaxErrors.Where(e => e.Message.Contains("Tokens can't be used within directives")).ToList();
        Assert.That(tokenErrors.Select(e => e.LineNumber), Is.EqualTo(new[] { 3 }));
        Assert.That(tokenErrors[0].Detail, Is.EqualTo("{{##VERSION}}.sql"));
    }
    #endregion
}
