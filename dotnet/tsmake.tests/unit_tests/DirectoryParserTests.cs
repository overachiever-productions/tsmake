namespace tsmake.tests.unit_tests;

[TestFixture]
public class DirectoryParserTests
{
    #region Paths
    [TestCase(@"Common\Tables", @"Common\Tables")]
    [TestCase(@"D:\My Scripts\Common Tables", @"D:\My Scripts\Common Tables")]
    [TestCase(@"~\Common\Tables", @"~\Common\Tables")]
    [TestCase(@"\\server\share\Tables", @"\\server\share\Tables")]
    [TestCase(@"   Common\Tables   ", @"Common\Tables")]
    [TestCase(@"""D:\Scripts (old), v2\Tables""", @"D:\Scripts (old), v2\Tables")]
    [TestCase(@"'D:\Scripts (old), v2\Tables'", @"D:\Scripts (old), v2\Tables")]
    [TestCase(@"""D:\My Scripts""", @"D:\My Scripts")]
    [TestCase(@"""D:\Has [exclude: brackets]""", @"D:\Has [exclude: brackets]")]
    [TestCase(@"D:\Scripts\[Archive]\Tables", @"D:\Scripts\[Archive]\Tables")]
    public void Parses_Path_Only(string payload, string expectedPath)
    {
        var result = DirectoryParser.Parse(payload);

        Assert.That(result.Path, Is.EqualTo(expectedPath));
        Assert.That(result.HasExclude, Is.False);
        Assert.That(result.HasSort, Is.False);
        Assert.That(result.Exclude, Is.Null);
        Assert.That(result.Sort, Is.Null);
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.None));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(-1));
    }

    [TestCase(@"Common\Tables [exclude: a.sql]")]
    [TestCase(@"Common\Tables[exclude: a.sql]")]
    [TestCase(@"Common\Tables     [exclude: a.sql]")]
    [TestCase(@"""Common\Tables"" [exclude: a.sql]")]
    [TestCase(@"'Common\Tables'[exclude: a.sql]")]
    public void Path_Ends_Where_First_Clause_Begins(string payload)
    {
        var result = DirectoryParser.Parse(payload);

        Assert.That(result.Path, Is.EqualTo(@"Common\Tables"));
        Assert.That(result.Exclude, Is.EqualTo(new[] { "a.sql" }));
    }
    #endregion

    #region [exclude:]
    [Test]
    public void Parses_Exclude_Entries_Quoted_And_Unquoted()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [exclude: a.sql, b c.sql ,""d, e.sql"", 'f (1).sql', ""g].sql""]");

        Assert.That(result.HasExclude, Is.True);
        Assert.That(result.HasSort, Is.False);
        Assert.That(result.Exclude, Is.EqualTo(new[] { "a.sql", "b c.sql", "d, e.sql", "f (1).sql", "g].sql" }));
    }

    [Test]
    public void Exclude_Keyword_Is_Case_And_Whitespace_Insensitive()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [ EXCLUDE : a.SQL ]");

        Assert.That(result.Exclude, Is.EqualTo(new[] { "a.SQL" }));
    }

    [Test]
    public void Parses_Wildcard_Entries_As_Literal_Strings()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [exclude: *.sql, test_??.sql, 'old (*).sql'] [sort: 00_*.sql, ALPHA, zz?.sql]");

        Assert.That(result.Exclude, Is.EqualTo(new[] { "*.sql", "test_??.sql", "old (*).sql" }));
        Assert.That(result.Sort, Is.EqualTo(new[] { "00_*.sql", "zz?.sql" }));
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.Alpha));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(1));
    }
    #endregion

    #region [sort:]
    [TestCase("ALPHA", DirectorySortMarker.Alpha)]
    [TestCase("alpha", DirectorySortMarker.Alpha)]
    [TestCase("alphaDESC", DirectorySortMarker.AlphaDesc)]
    [TestCase("ALPHADESC", DirectorySortMarker.AlphaDesc)]
    [TestCase("Timestamp", DirectorySortMarker.Timestamp)]
    [TestCase("TimestampDESC", DirectorySortMarker.TimestampDesc)]
    [TestCase("timestampdesc", DirectorySortMarker.TimestampDesc)]
    public void Parses_Marker_Only_Sort(string marker, DirectorySortMarker expected)
    {
        var result = DirectoryParser.Parse($@"Common\Tables [sort: {marker}]");

        Assert.That(result.HasSort, Is.True);
        Assert.That(result.Sort, Is.Empty);
        Assert.That(result.SortMarker, Is.EqualTo(expected));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(0));
    }

    [Test]
    public void Parses_Sort_Names_Without_Marker()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [sort: 01.setup.sql, 02 tables.sql, ""03, views.sql"", '04 (procs).sql']");

        Assert.That(result.Sort, Is.EqualTo(new[] { "01.setup.sql", "02 tables.sql", "03, views.sql", "04 (procs).sql" }));
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.None));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(-1));
    }

    [Test]
    public void Records_Marker_Position_Among_Sort_Names()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [sort: first.sql, second.sql, ALPHAdesc, last.sql]");

        Assert.That(result.Sort, Is.EqualTo(new[] { "first.sql", "second.sql", "last.sql" }));
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.AlphaDesc));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(2));
    }

    [Test]
    public void Quoted_Marker_Is_A_Literal_Name()
    {
        var result = DirectoryParser.Parse(@"Common\Tables [sort: 'ALPHA', ""timestamp"", TIMESTAMP]");

        Assert.That(result.Sort, Is.EqualTo(new[] { "ALPHA", "timestamp" }));
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.Timestamp));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(2));
    }
    #endregion

    #region Both Clauses
    [TestCase(@"""D:\My Scripts"" [exclude: x.sql, y.sql] [sort: 00_setup.sql, ALPHA]")]
    [TestCase(@"""D:\My Scripts"" [sort: 00_setup.sql, ALPHA] [exclude: x.sql, y.sql]")]
    [TestCase(@"'D:\My Scripts'[sort:00_setup.sql,ALPHA][exclude:x.sql,y.sql]")]
    public void Parses_Both_Clauses_In_Either_Order(string payload)
    {
        var result = DirectoryParser.Parse(payload);

        Assert.That(result.Path, Is.EqualTo(@"D:\My Scripts"));
        Assert.That(result.Exclude, Is.EqualTo(new[] { "x.sql", "y.sql" }));
        Assert.That(result.Sort, Is.EqualTo(new[] { "00_setup.sql" }));
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.Alpha));
        Assert.That(result.SortMarkerPosition, Is.EqualTo(1));
    }

    [Test]
    public void Parses_Payload_From_Full_Directive_Line()
    {
        var line = @"-- ##DIRECTORY: Common\My Tables [exclude: scratch.sql] [sort: TimestampDESC]   ##:: vNEXT: something";

        System.Text.RegularExpressions.Match m = Global.DirectiveRegex.Match(line);
        Assert.That(m.Success, Is.True);
        Assert.That(m.Groups["directive"].Value, Is.EqualTo("DIRECTORY"));

        var result = DirectoryParser.Parse(m.Groups["data"].Value.StripDirectiveComments());

        Assert.That(result.Path, Is.EqualTo(@"Common\My Tables"));
        Assert.That(result.Exclude, Is.EqualTo(new[] { "scratch.sql" }));
        Assert.That(result.Sort, Is.Empty);
        Assert.That(result.SortMarker, Is.EqualTo(DirectorySortMarker.TimestampDesc));
    }
    #endregion

    #region Errors
    [TestCase("", "path is required")]
    [TestCase("    ", "path is required")]
    [TestCase(@"""""", "quoted path is empty")]
    [TestCase(@"""   "" [sort: ALPHA]", "quoted path is empty")]
    [TestCase(@"[sort: ALPHA]", "path is required before")]
    [TestCase(@"D:\Scripts, v2", "must be wrapped")]
    [TestCase(@"D:\Scripts (old)", "must be wrapped")]
    [TestCase(@"D:\O'Brien\Tables", "must be wrapped")]
    [TestCase(@"""D:\Scripts", "missing its closing")]
    [TestCase(@"""D:\Scripts"" extra", "may follow the quoted path")]
    [TestCase(@"Tables [exlude: a.sql]", "Unknown clause '[exlude:'")]
    [TestCase(@"Tables [exlude: a.sql, b.sql]", "Unknown clause '[exlude:'")]
    [TestCase(@"""Tables"" [srt: ALPHA]", "Unknown clause '[srt:'")]
    [TestCase(@"Tables [exclude: a.sql] [srt: ALPHA]", "Unknown clause '[srt:'")]
    [TestCase(@"Tables [exclude: a.sql", "missing its closing ']'")]
    [TestCase(@"Tables [exclude: ""a.sql]", "missing its closing ']'")]
    [TestCase(@"Tables [sort: ALPHA] trailing junk", "or the end of the directive")]
    [TestCase(@"Tables [exclude: a.sql] [exclude: b.sql]", "Only one [exclude: ...]")]
    [TestCase(@"Tables [sort: ALPHA] [sort: b.sql]", "Only one [sort: ...]")]
    [TestCase(@"Tables [exclude: a.sql, readme.md]", "must be .sql files; found: 'readme.md'")]
    [TestCase(@"Tables [exclude: ALPHA]", "must be .sql files")]
    [TestCase(@"Tables [exclude: test_*]", "must be .sql files; found: 'test_*'")]
    [TestCase(@"Tables [exclude: *]", "must be .sql files")]
    [TestCase(@"Tables [sort: ALPHA, timestamp]", "Only one sort marker")]
    [TestCase(@"Tables [exclude: ]", "clause is empty")]
    [TestCase(@"Tables [sort:]", "clause is empty")]
    [TestCase(@"Tables [exclude: a.sql, ]", "trailing comma")]
    [TestCase(@"Tables [exclude: a.sql,, b.sql]", "two commas in a row")]
    [TestCase(@"Tables [exclude: , a.sql]", "leading comma")]
    [TestCase(@"Tables [exclude: a.sql, '']", "Empty quoted entry")]
    [TestCase(@"Tables [exclude: a(1).sql]", "must be wrapped")]
    [TestCase(@"Tables [sort: 'a.sql' b.sql]", "must be wrapped")]
    public void Invalid_Syntax_Throws_FormatException(string payload, string expectedMessageFragment)
    {
        var ex = Assert.Throws<FormatException>(() => DirectoryParser.Parse(payload));
        StringAssert.Contains(expectedMessageFragment, ex!.Message);
    }

    [Test]
    public void Null_Payload_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DirectoryParser.Parse(null!));
    }
    #endregion
}
