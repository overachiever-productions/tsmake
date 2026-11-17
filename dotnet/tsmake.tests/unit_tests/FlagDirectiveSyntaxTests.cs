namespace tsmake.tests.unit_tests;

// Malformed ##FLAG-FILE / ##FLAG-DIRECTORY lines (and ##FLAG, ##FLAG-DEFAULT, ##FLAG-END) are REPORTED - not silently ignored as comments.
[TestFixture]
public class FlagDirectiveSyntaxTests
{
    #region DirectiveRegex
    // Well-formed directives: same 'directive' and 'data' groups as before malformed FLAG lines were matched too.
    [TestCase(@"-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT): onprem.sql", "FLAG-FILE(#Azure)", "azure.sql (DEFAULT): onprem.sql")]
    [TestCase(@"-- ##FLAGS-FILE(#On_Prem-2) : onprem.sql", "FLAGS-FILE(#On_Prem-2) ", "onprem.sql")]
    [TestCase(@"-- ##FLAG-FILE(#RDS, #Azure):cloud.sql", "FLAG-FILE(#RDS, #Azure)", "cloud.sql")]
    [TestCase(@"  --   ##flag-directory ( #azure ) : ~\azure\ (default): ~\onprem\", "flag-directory ( #azure ) ", @"~\azure\ (default): ~\onprem\")]
    [TestCase(@"-- ##FILE: child.sql", "FILE", "child.sql")]
    [TestCase(@"-- ##FILE-MARKER: ~\{version}.md [body: hi]", "FILE-MARKER", @"~\{version}.md [body: hi]")]
    [TestCase(@"-- ##DIRECTORY: Common [sort: ALPHA]", "DIRECTORY", "Common [sort: ALPHA]")]
    [TestCase(@"-- ##:: a note", ":", "a note")]
    [TestCase(@"-- ##CONDITION-END:", "CONDITION-END", "")]
    [TestCase(@"-- ##OUTPUT: latest.sql", "OUTPUT", "latest.sql")]
    public void Well_Formed_Directives_Are_Unchanged(string line, string expectedDirective, string expectedData)
    {
        var m = Global.DirectiveRegex.Match(line);

        Assert.That(m.Success, Is.True);
        Assert.That(m.Groups["directive"].Value, Is.EqualTo(expectedDirective));
        Assert.That(m.Groups["data"].Value, Is.EqualTo(expectedData));
    }

    // Malformed FLAG lines now MATCH - so they're loaded (and reported) instead of being treated as ordinary comments.
    [TestCase(@"-- ##FLAG-FILE: azure.sql")]
    [TestCase(@"-- ##FLAG-FILE")]
    [TestCase(@"-- ##FLAG-FILE(#Azure)  : azure.sql")]
    [TestCase(@"-- ##FLAG-FILE(#Azure) azure.sql")]
    [TestCase(@"-- ##FLAG-FILE(#My Flag): azure.sql")]
    [TestCase(@"-- ##FLAG-DIRECTORY(#Azure)  : funcs")]
    [TestCase(@"-- ##FLAG-DIRECTORY funcs")]
    [TestCase(@"-- ##FLAG(#Azure): azure.sql")]
    [TestCase(@"-- ##FLAGS (#Azure): azure.sql")]
    [TestCase(@"-- ##FLAG: azure.sql")]
    [TestCase(@"-- ##FLAG-DEFAULT:")]
    [TestCase(@"-- ##flags-end:")]
    public void Malformed_Flag_Lines_Are_Matched(string line)
    {
        Assert.That(Global.DirectiveRegex.IsMatch(line), Is.True);
    }

    // Still NOT directives (i.e., ordinary comments): a space after ##, other names that merely START with FLAG, and ##FLAG without '(' or ':'.
    [TestCase(@"-- ## FLAG-FILE(#Azure): azure.sql")]
    [TestCase(@"-- ##FLAG-FILES: azure.sql")]
    [TestCase(@"-- ##FLAG-DIRECTORYX(#Azure): funcs")]
    [TestCase(@"-- ##FLAGGED: needs review")]
    [TestCase(@"-- ##FLAG this needs review")]
    [TestCase(@"-- ##FLAG-DEFAULTS: x")]
    [TestCase(@"-- ##FOO: x")]
    public void Other_Lines_Are_Not_Matched(string line)
    {
        Assert.That(Global.DirectiveRegex.IsMatch(line), Is.False);
    }
    #endregion

    #region Builds (end-to-end)
    private static Assembler Build(string directiveLine, params string[] flags)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "azure.sql"), "PRINT 'azure';");
            File.WriteAllText(Path.Combine(temp, "onprem.sql"), "PRINT 'onprem';");
            Directory.CreateDirectory(Path.Combine(temp, "funcs"));
            File.WriteAllText(Path.Combine(temp, "funcs", "a.sql"), "PRINT 'funcs a';");

            string buildPath = Path.Combine(temp, "test.build.sql");
            File.WriteAllText(buildPath, $"-- ##OUTPUT: test_latest.sql\r\n{directiveLine}\r\nPRINT 'end';");

            var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
            options.AddRootPath(new RankedString(SourceType.BuildFile, temp));
            if (flags.Length > 0)
                options.SetFlags(flags.ToList(), FlagsSource.Console);

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

    [TestCase(@"-- ##FLAG-FILE: azure.sql", "Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-FILE")]
    [TestCase(@"-- ##FLAG-FILE: Odd (v2)\azure.sql", "Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-FILE")]
    [TestCase(@"-- ##FLAG-FILE", "No (#FLAG) or (DEFAULT) options found.")]
    [TestCase(@"-- ##FLAG-FILE   ##:: no options yet", "No (#FLAG) or (DEFAULT) options found.")]
    [TestCase(@"-- ##FLAG-FILE(#Azure)  : azure.sql", "Missing ':' after '(#Azure)' (at most one space is allowed before it).")]
    [TestCase(@"-- ##FLAG-FILE(#Azure) azure.sql", "Missing ':' after '(#Azure)'")]
    [TestCase(@"-- ##FLAG-FILE(#My Flag): azure.sql", "names may only contain A-Z, a-z, 0-9, '-' and '_'.")]
    [TestCase(@"-- ##FLAG-DIRECTORY(#Azure)  : funcs", "Missing ':' after '(#Azure)' (at most one space is allowed before it).")]
    [TestCase(@"-- ##FLAG-DIRECTORY funcs", "Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-DIRECTORY")]
    [TestCase(@"-- ##FLAG(#Azure): azure.sql", "Directive 'FLAG' is not valid. Use FLAG-FILE or FLAG-DIRECTORY instead.")]
    [TestCase(@"-- ##FLAGS: azure.sql", "Directive 'FLAGS' is not valid. Use FLAG-FILE or FLAG-DIRECTORY instead.")]
    [TestCase(@"-- ##FLAG-DEFAULT:", "Directive 'FLAG-DEFAULT' is not valid. Use CONDITION-DEFAULT instead.")]
    [TestCase(@"-- ##flag-end:", "Directive 'flag-end' is not valid. Use CONDITION-END instead.")]
    public void Malformed_Flag_Lines_Are_Reported_Against_Their_Line(string directiveLine, string expectedMessage)
    {
        var assembler = Build(directiveLine, "#Azure");

        var error = assembler.SyntaxErrors.Single();
        StringAssert.Contains(expectedMessage, error.Message);
        Assert.That(error.Message, Does.Not.Contain("(Parameter '"));
        Assert.That(error.LineNumber, Is.EqualTo(2));

        // ... and (like any other invalid directive) the line is NOT part of the output:
        Assert.That(assembler.ArtifactLines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "PRINT 'end';" }));
    }

    [TestCase(new string[0], "PRINT 'onprem';")]
    [TestCase(new[] { "#Azure" }, "PRINT 'azure';")]
    public void Well_Formed_Flag_File_Still_Works(string[] flags, string expected)
    {
        var assembler = Build(@"-- ##FLAG-FILE (#Azure) : azure.sql (DEFAULT): onprem.sql   ##:: a note", flags);

        Assert.That(assembler.SyntaxErrors, Is.Empty);
        Assert.That(assembler.ArtifactLines.Select(l => l.OriginalContent), Is.EqualTo(new[] { expected, "PRINT 'end';" }));
    }

    [Test]
    public void Well_Formed_Flag_Directory_Still_Works()
    {
        var assembler = Build(@"-- ##FLAG-DIRECTORY(#Azure): funcs", "#Azure");

        Assert.That(assembler.SyntaxErrors, Is.Empty);
        Assert.That(assembler.ArtifactLines.Select(l => l.OriginalContent), Is.EqualTo(new[] { "PRINT 'funcs a';", "PRINT 'end';" }));
    }

    [TestCase(@"-- ## FLAG-FILE(#Azure): azure.sql")]
    [TestCase(@"-- ##FLAG this needs review")]
    public void Non_Directives_Stay_Ordinary_Comments(string line)
    {
        var assembler = Build(line, "#Azure");

        Assert.That(assembler.SyntaxErrors, Is.Empty);
        Assert.That(assembler.ArtifactLines.Select(l => l.OriginalContent), Is.EqualTo(new[] { line, "PRINT 'end';" }));
    }
    #endregion
}
