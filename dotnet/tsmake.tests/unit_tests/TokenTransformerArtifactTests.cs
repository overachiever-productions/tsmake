namespace tsmake.tests.unit_tests;

// TokenTransformer.TransformBuildArtifacts(): tokens in the ##OUTPUT path, and in the ##FILE-MARKER path + [body:].
[TestFixture]
public class TokenTransformerArtifactTests
{
    private static readonly (string Name, string Value)[] DefaultTokens =
    [
        ("VERSION", "14.8.5629.2"),
        ("SUMMARY", "Kill-Store's fixes; \"quoted\" [bracketed]"),
        ("PROJECT", "S4"),
        ("EMPTY", ""),
        ("NESTED", "{{##VERSION}}")
    ];

    private const TokenExclusionDirectives AllExclusions =
        TokenExclusionDirectives.ExcludeStrings | TokenExclusionDirectives.ExcludeEolComments | TokenExclusionDirectives.ExcludeBlockComments;

    private static List<ISyntaxError> Transform(List<IArtifact> artifacts, TokenExclusionDirectives exclusions = TokenExclusionDirectives.None, (string Name, string Value)[]? tokens = null)
    {
        var registry = TokenDefinitionRegistry.CreateRegistry((tokens ?? DefaultTokens).Select(t => new TokenDefinition(t.Name, t.Value)));
        var errors = new List<ISyntaxError>();
        new TokenTransformer(exclusions, registry).TransformBuildArtifacts(artifacts, errors);
        return errors;
    }

    // A BuildArtifact from a real ##OUTPUT directive line (so: with its line number for errors).
    private static BuildArtifact Output(string path, int lineNumber = 3)
    {
        string lineText = "-- ##OUTPUT: " + path;
        var codeLine = new CodeLine(lineText, lineNumber, 0, lineText.Length, new FileDetail("s4.build.sql", 0, 1));
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.TranslatePath(It.IsAny<string>())).Returns<string>(p => p);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var m = Global.DirectiveRegex.Match(lineText);
        var directive = (OutputDirective)DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem.Object);
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

        return new BuildArtifact(directive.Path, SourceType.BuildFile, directive);
    }

    // A FileMarkerArtifact from a real ##FILE-MARKER directive line (so: with its [body:] - and its line number for errors).
    private static FileMarkerArtifact Marker(string payload, int lineNumber = 4)
    {
        string lineText = "-- ##FILE-MARKER: " + payload;
        var codeLine = new CodeLine(lineText, lineNumber, 0, lineText.Length, new FileDetail("s4.build.sql", 0, 1));
        var m = Global.DirectiveRegex.Match(lineText);
        var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, new Mock<IFileSystem>().Object);
        Assert.That(directive.IsValid, Is.True, directive.ValidationMessage);

        var marker = (FileMarkerDirective)directive;
        return new FileMarkerArtifact(marker.Path, SourceType.BuildFile, marker);
    }

    #region No artifacts
    [Test]
    public void No_Artifacts_Is_A_No_Op()
    {
        Assert.That(Transform(new List<IArtifact>()), Is.Empty);
    }

    [Test]
    public void Null_Artifacts_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Transform(null!));
    }
    #endregion

    #region ##OUTPUT (BuildArtifact.Path)
    [TestCase(@"D:\builds\s4_{{##VERSION}}.sql", @"D:\builds\s4_14.8.5629.2.sql")]
    [TestCase(@"D:\builds\{{##PROJECT}}\{{##PROJECT}}_{{##VERSION}}.sql", @"D:\builds\S4\S4_14.8.5629.2.sql")]
    [TestCase(@"D:\builds\s4{{##EMPTY}}_latest.sql", @"D:\builds\s4_latest.sql")]
    [TestCase(@"D:\builds\s4_latest.sql", @"D:\builds\s4_latest.sql")]
    [TestCase(@"D:\builds\s4_{{##version}}.sql", @"D:\builds\s4_{{##version}}.sql")]          // not a token (lower-case): just text.
    [TestCase(@"D:\builds\{{##NESTED}}.sql", @"D:\builds\{{##VERSION}}.sql")]               // single pass.
    public void Output_Path_Tokens_Are_Replaced(string path, string expected)
    {
        var output = Output(path);

        var errors = Transform([output]);

        Assert.That(errors, Is.Empty);
        Assert.That(output.Path, Is.EqualTo(expected));
    }

    [Test]
    public void Undefined_Output_Path_Token_Is_An_Error_Against_The_Directive_Line()
    {
        var output = Output(@"D:\builds\s4_{{##VERSION}}_{{##BUILD-DATE}}.sql", lineNumber: 5);

        var error = Transform([output]).Single();

        Assert.That(output.Path, Is.EqualTo(@"D:\builds\s4_14.8.5629.2_{{##BUILD-DATE}}.sql"));      // left as-is.
        Assert.That(error.Message, Does.StartWith("Undefined token in the ##OUTPUT path: {{##BUILD-DATE}}."));
        Assert.That(error.Detail, Is.EqualTo(@"D:\builds\s4_{{##VERSION}}_{{##BUILD-DATE}}.sql"));
        Assert.That(error.LineNumber, Is.EqualTo(5));
        Assert.That(error.FileName, Is.EqualTo("s4.build.sql"));
    }

    [Test]
    public void Output_Without_A_Directive_Reports_Without_A_Line_Number()
    {
        // e.g., an output path passed in by the caller.
        var output = new BuildArtifact(@"D:\builds\s4_{{##BUILD-DATE}}.sql", SourceType.CommandLine, null!);

        var error = Transform([output]).Single();

        Assert.That(error.LineNumber, Is.EqualTo(0));
        Assert.That(error.FileName, Is.EqualTo("(CommandLine)"));
    }

    [Test]
    public void Undefined_Tokens_Are_Errors_Even_With_No_Tokens_Defined()
    {
        var output = Output(@"D:\builds\s4_{{##VERSION}}.sql");

        var error = Transform([output], tokens: []).Single();

        Assert.That(output.Path, Is.EqualTo(@"D:\builds\s4_{{##VERSION}}.sql"));
        Assert.That(error.Message, Does.StartWith("Undefined token in the ##OUTPUT path: {{##VERSION}}."));
    }
    #endregion

    #region ##FILE-MARKER (FileMarkerArtifact.Path + .ArtifactBody)
    [Test]
    public void Marker_Path_And_Body_Tokens_Are_Replaced()
    {
        var marker = Marker(@"~\{{##VERSION}}.marker.md [body:# {{##PROJECT}} - {{##VERSION}}`r`n{{##SUMMARY}}]");

        var errors = Transform([marker]);

        Assert.That(errors, Is.Empty);
        Assert.That(marker.Path, Is.EqualTo(@"~\14.8.5629.2.marker.md"));
        Assert.That(marker.ArtifactBody, Is.EqualTo("# S4 - 14.8.5629.2`r`nKill-Store's fixes; \"quoted\" [bracketed]"));
    }

    [Test]
    public void Marker_Without_Body_Keeps_A_Null_Body()
    {
        var marker = Marker(@"~\{{##VERSION}}.marker.md");

        Assert.That(Transform([marker]), Is.Empty);
        Assert.That(marker.Path, Is.EqualTo(@"~\14.8.5629.2.marker.md"));
        Assert.That(marker.ArtifactBody, Is.Null);
    }

    // The body is PLAIN TEXT (markdown) - NOT T-SQL: apostrophes, -- and /* */ don't start strings or comments, so -TokenExclusions
    //      never apply - and values are never escaped (no '' ]] or "" doubling).
    [TestCase("Here's {{##SUMMARY}}", "Here's Kill-Store's fixes; \"quoted\" [bracketed]")]
    [TestCase("-- {{##VERSION}}", "-- 14.8.5629.2")]
    [TestCase("/* {{##VERSION}} */", "/* 14.8.5629.2 */")]
    [TestCase("'{{##SUMMARY}}' [{{##SUMMARY}}] \"{{##SUMMARY}}\"", "'Kill-Store's fixes; \"quoted\" [bracketed]' [Kill-Store's fixes; \"quoted\" [bracketed]] \"Kill-Store's fixes; \"quoted\" [bracketed]\"")]
    public void Marker_Body_Is_Plain_Text(string body, string expected)
    {
        var marker = Marker($@"~\x.md [body:{body}]");

        var errors = Transform([marker], AllExclusions);

        Assert.That(errors, Is.Empty);
        Assert.That(marker.ArtifactBody, Is.EqualTo(expected));
    }

    [Test]
    public void Multi_Line_Body_Is_Replaced_Throughout()
    {
        // e.g., once `r`n has been turned into real line breaks. (A directive's [body:] is single-line - and SetBody() is internal.)
        var marker = Marker(@"~\x.md [body: placeholder]");
        typeof(FileMarkerArtifact).GetProperty(nameof(FileMarkerArtifact.ArtifactBody))!
            .SetValue(marker, "# {{##PROJECT}}\r\n\r\n'{{##VERSION}}\r\n{{##SUMMARY}}\n-- {{##VERSION}}");

        Assert.That(Transform([marker], AllExclusions), Is.Empty);
        Assert.That(marker.ArtifactBody, Is.EqualTo("# S4\r\n\r\n'14.8.5629.2\r\nKill-Store's fixes; \"quoted\" [bracketed]\n-- 14.8.5629.2"));
    }

    [Test]
    public void Undefined_Marker_Tokens_Are_Errors_Against_The_Directive_Line()
    {
        var marker = Marker(@"~\{{##BRANCH}}\{{##VERSION}}.md [body:# {{##VERSION}} {{##NOTES}} {{##NOTES}} {{##AUTHOR}}]", lineNumber: 7);

        var errors = Transform([marker]);

        Assert.That(errors.Select(e => e.Message.Substring(0, e.Message.IndexOf(". Define", StringComparison.Ordinal))), Is.EqualTo(new[]
        {
            "Undefined token in the ##FILE-MARKER path: {{##BRANCH}}",
            "Undefined tokens in the ##FILE-MARKER [body:]: {{##NOTES}}, {{##AUTHOR}}"      // each distinct token - once.
        }));
        Assert.That(errors.Select(e => e.LineNumber), Is.EqualTo(new[] { 7, 7 }));
        Assert.That(errors.Select(e => e.FileName), Is.EqualTo(new[] { "s4.build.sql", "s4.build.sql" }));

        // Defined tokens are still replaced; undefined ones are left as-is:
        Assert.That(marker.Path, Is.EqualTo(@"~\{{##BRANCH}}\14.8.5629.2.md"));
        Assert.That(marker.ArtifactBody, Is.EqualTo("# 14.8.5629.2 {{##NOTES}} {{##NOTES}} {{##AUTHOR}}"));
    }

    [Test]
    public void Marker_Without_A_Directive_Reports_Without_A_Line_Number()
    {
        var marker = new FileMarkerArtifact(@"~\{{##BRANCH}}.md", SourceType.CommandLine, null!);

        var error = Transform([marker]).Single();

        Assert.That(error.LineNumber, Is.EqualTo(0));
        Assert.That(error.FileName, Is.EqualTo("(CommandLine)"));
        Assert.That(marker.ArtifactBody, Is.Null);
    }
    #endregion

    [Test]
    public void Every_Artifact_Is_Processed()
    {
        var output = Output(@"D:\builds\s4_{{##VERSION}}.sql");
        var marker = Marker(@"D:\builds\{{##VERSION}}.marker.md [body:v{{##VERSION}}]");

        Assert.That(Transform([output, marker]), Is.Empty);
        Assert.That(output.Path, Is.EqualTo(@"D:\builds\s4_14.8.5629.2.sql"));
        Assert.That(marker.Path, Is.EqualTo(@"D:\builds\14.8.5629.2.marker.md"));
        Assert.That(marker.ArtifactBody, Is.EqualTo("v14.8.5629.2"));
    }
}
