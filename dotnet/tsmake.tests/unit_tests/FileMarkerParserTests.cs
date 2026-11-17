namespace tsmake.tests.unit_tests;

[TestFixture]
public class FileMarkerParserTests
{
    // The example from the spec - as it'd be written in a build file (`r`n are literal text in the .sql file):
    private const string ExampleLine = @"--##FILE-MARKER: ~\{version}.marker.md [body:![](https://assets.overachiever.net/s4/images/s4_main_logo.png)`r`n`r`n# S4 - {version}`r`n`r`n{summary}`r`n`r`nFor more information see the [change log](/changelog.md) or check out the [readme](/readme.md) for this project.]";
    private const string ExampleBody = @"![](https://assets.overachiever.net/s4/images/s4_main_logo.png)`r`n`r`n# S4 - {version}`r`n`r`n{summary}`r`n`r`nFor more information see the [change log](/changelog.md) or check out the [readme](/readme.md) for this project.";

    // Same steps as the DirectiveProcessor + BaseDirective: match the line, take its data, remove any ##:: comment, trim.
    private static FileMarkerOptions ParseLine(string line)
    {
        var m = Global.DirectiveRegex.Match(line);
        Assert.That(m.Success, Is.True, "Not a directive line.");
        Assert.That(m.Groups["directive"].Value, Is.EqualTo("FILE-MARKER").IgnoreCase);

        return FileMarkerParser.Parse(m.Groups["data"].Value.StripDirectiveComments().Trim());
    }

    #region Paths
    [TestCase(@"~\{version}.marker.md", @"~\{version}.marker.md")]
    [TestCase(@"marker.md", @"marker.md")]
    [TestCase(@"D:\My Markers\S4 {version} marker.md", @"D:\My Markers\S4 {version} marker.md")]
    [TestCase(@"\\server\share\{version}.md", @"\\server\share\{version}.md")]
    [TestCase(@"   ~\{version}.marker.md   ", @"~\{version}.marker.md")]
    [TestCase(@"~\[Archive]\{version}.md", @"~\[Archive]\{version}.md")]
    [TestCase(@"~\markers (old), v2\x.md", @"~\markers (old), v2\x.md")]
    public void Parses_Path_Only(string payload, string expectedPath)
    {
        var result = FileMarkerParser.Parse(payload);

        Assert.That(result.FilePath, Is.EqualTo(expectedPath));
        Assert.That(result.Body, Is.Null);
        Assert.That(result.HasBody, Is.False);
    }

    [TestCase(@"~\{version}.marker.md [body: hello]")]
    [TestCase(@"~\{version}.marker.md[body: hello]")]
    [TestCase(@"~\{version}.marker.md      [body: hello]")]
    [TestCase(@"  ~\{version}.marker.md [body: hello]  ")]
    public void Path_Ends_Where_Body_Begins(string payload)
    {
        var result = FileMarkerParser.Parse(payload);

        Assert.That(result.FilePath, Is.EqualTo(@"~\{version}.marker.md"));
        Assert.That(result.Body, Is.EqualTo("hello"));
        Assert.That(result.HasBody, Is.True);
    }

    [Test]
    public void Path_With_Brackets_Before_Body()
    {
        var result = FileMarkerParser.Parse(@"~\[Archive]\{version}.md [body: hello]");

        Assert.That(result.FilePath, Is.EqualTo(@"~\[Archive]\{version}.md"));
        Assert.That(result.Body, Is.EqualTo("hello"));
    }
    #endregion

    #region [body:]
    [Test]
    public void Parses_Spec_Example()
    {
        var result = ParseLine(ExampleLine);

        Assert.That(result.FilePath, Is.EqualTo(@"~\{version}.marker.md"));
        Assert.That(result.Body, Is.EqualTo(ExampleBody));
    }

    [TestCase(@"~\x.md [body:hello]")]
    [TestCase(@"~\x.md [BODY:hello]")]
    [TestCase(@"~\x.md [Body:hello]")]
    [TestCase(@"~\x.md [ body : hello]")]
    [TestCase(@"~\x.md [body:   hello   ]")]
    public void Body_Keyword_Is_Case_And_Whitespace_Insensitive_And_Body_Is_Trimmed(string payload)
    {
        var result = FileMarkerParser.Parse(payload);

        Assert.That(result.FilePath, Is.EqualTo(@"~\x.md"));
        Assert.That(result.Body, Is.EqualTo("hello"));
    }

    [TestCase(@"~\x.md [body: ![](logo.png) [a] [b] [link](/x.md)]", @"![](logo.png) [a] [b] [link](/x.md)")]
    [TestCase(@"~\x.md [body: see [1]]", @"see [1]")]
    [TestCase(@"~\x.md [body: ]]]", @"]]")]
    [TestCase(@"~\x.md [body: [[nested]]]", @"[[nested]]")]
    [TestCase(@"~\x.md [body: unbalanced [ or ] are fine]", @"unbalanced [ or ] are fine")]
    public void Body_Can_Contain_Brackets(string payload, string expectedBody)
    {
        Assert.That(FileMarkerParser.Parse(payload).Body, Is.EqualTo(expectedBody));
    }

    [Test]
    public void Body_Can_Contain_Body_Opener()
    {
        var result = FileMarkerParser.Parse(@"~\x.md [body: the [body: ...] tag]");

        Assert.That(result.FilePath, Is.EqualTo(@"~\x.md"));
        Assert.That(result.Body, Is.EqualTo("the [body: ...] tag"));
    }

    [TestCase(@"S4 -- {version} `r`n## Summary`r`n{summary}")]
    [TestCase(@"'single' ""double"" (parens), commas; semi: colons")]
    [TestCase(@"{{##NOT-PROCESSED-HERE}} {0} {version} {summary}")]
    [TestCase(@"> [!NOTE]`r`n> marker-only file.")]
    [TestCase(@"--##FILE: not a directive")]
    public void Body_Is_Returned_As_Is(string body)
    {
        Assert.That(FileMarkerParser.Parse($@"~\x.md [body:{body}]").Body, Is.EqualTo(body));
    }

    // As specified: the body ends at the LAST ']' on the line. So a MISSING closing ']' can't be detected when the body itself ends with ']'.
    [Test]
    public void Body_Ends_At_The_Last_Bracket_On_The_Line()
    {
        Assert.That(FileMarkerParser.Parse(@"~\x.md [body: see [readme]").Body, Is.EqualTo("see [readme"));
    }
    #endregion

    #region Directive Lines (DirectiveRegex + ##:: comments)
    [TestCase(@"--##FILE-MARKER: ~\x.md [body: hello]")]
    [TestCase(@"-- ##FILE-MARKER: ~\x.md [body: hello]")]
    [TestCase(@"   --   ##file-marker:    ~\x.md [body: hello]   ")]
    [TestCase(@"--##FILE-MARKER: ~\x.md [body: hello] ##:: a directive comment - AFTER the closing ']'")]
    [TestCase(@"--##FILE-MARKER: ~\x.md [body: hello]##::no space")]
    public void Parses_Directive_Lines(string line)
    {
        var result = ParseLine(line);

        Assert.That(result.FilePath, Is.EqualTo(@"~\x.md"));
        Assert.That(result.Body, Is.EqualTo("hello"));
    }

    [Test]
    public void Directive_Comment_Within_Body_Truncates_It()
    {
        // ##:: anywhere on the line starts a directive comment - which is removed BEFORE parsing. So the body loses its closing ']'.
        var ex = Assert.Throws<FormatException>(() => ParseLine(@"--##FILE-MARKER: ~\x.md [body: a ##:: b]"));
        StringAssert.Contains("missing its closing ']'", ex!.Message);
    }
    #endregion

    #region Errors
    [Test]
    public void Null_Payload_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FileMarkerParser.Parse(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Path_Is_Required(string payload)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        Assert.That(ex!.Message, Is.EqualTo("A ##FILE-MARKER path is required."));
    }

    [TestCase("[body: hello]")]
    [TestCase("   [body: hello]")]
    [TestCase("[ BODY : hello]")]
    public void Path_Is_Required_Before_Body(string payload)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        StringAssert.StartsWith("A ##FILE-MARKER path is required before the [body: ...] clause.", ex!.Message);
    }

    [TestCase(@"~\x.md [body: hello")]
    [TestCase(@"~\x.md [body:")]
    [TestCase(@"~\x.md [body: hello] extra")]
    [TestCase(@"~\x.md [body: see [readme](/readme.md)")]
    public void Body_Requires_Closing_Bracket_At_End(string payload)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        StringAssert.Contains("missing its closing ']' - which must be the LAST thing on the line.", ex!.Message);
    }

    [TestCase(@"~\x.md [body:]")]
    [TestCase(@"~\x.md [body:    ]")]
    public void Body_Can_Not_Be_Empty(string payload)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        StringAssert.StartsWith("The ##FILE-MARKER [body:] clause is empty.", ex!.Message);
    }

    [TestCase(@"~\x.md [bdy: hello]", "bdy")]
    [TestCase(@"~\x.md [title: a] [body: b]", "title")]
    [TestCase(@"~\x.md [ summary : a]", "summary")]
    [TestCase(@"[bdy: hello]", "bdy")]
    public void Unknown_Clauses_Throw(string payload, string kind)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        StringAssert.Contains($"Unknown clause '[{kind}:'. Expected [body: ...].", ex!.Message);
    }

    [TestCase("~\\x.md [body: line 1\r\nline 2]")]
    [TestCase("~\\x.md [body: line 1\nline 2]")]
    [TestCase("~\\x.md\r\n[body: hello]")]
    public void Must_Be_On_A_Single_Line(string payload)
    {
        var ex = Assert.Throws<FormatException>(() => FileMarkerParser.Parse(payload));
        StringAssert.StartsWith("A ##FILE-MARKER directive must be on a single line", ex!.Message);
    }
    #endregion
}
