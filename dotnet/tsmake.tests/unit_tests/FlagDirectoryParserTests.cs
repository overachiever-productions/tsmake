namespace tsmake.tests.unit_tests;

[TestFixture]
public class FlagDirectoryParserTests
{
    private const string Example = @"(#Azure): ~\azure-funcs\ [sort: azure_helper.sql, ALPHA] [exclude: _ignored.sql] (DEFAULT): ~\on-prem-funcs\ [sort: ALPHA]  ";

    #region Options
    [Test]
    public void Splits_Each_Option_Into_A_Directory_Payload()
    {
        var options = FlagDirectoryParser.Parse(Example);

        Assert.That(options.Keys, Is.EqualTo(new[] { "#AZURE", "DEFAULT" }));
        Assert.That(options["#AZURE"], Is.EqualTo(@"~\azure-funcs\ [sort: azure_helper.sql, ALPHA] [exclude: _ignored.sql]"));
        Assert.That(options["DEFAULT"], Is.EqualTo(@"~\on-prem-funcs\ [sort: ALPHA]"));
    }

    [Test]
    public void Each_Payload_Follows_The_Directory_Rules()
    {
        var options = FlagDirectoryParser.Parse(Example);

        var azure = DirectoryParser.Parse(options["#AZURE"]);
        Assert.That(azure.Path, Is.EqualTo(@"~\azure-funcs\"));
        Assert.That(azure.Exclude, Is.EqualTo(new[] { "_ignored.sql" }));
        Assert.That(azure.Sort, Is.EqualTo(new[] { "azure_helper.sql" }));
        Assert.That(azure.SortMarker, Is.EqualTo(DirectorySortMarker.Alpha));
        Assert.That(azure.SortMarkerPosition, Is.EqualTo(1));

        var onPrem = DirectoryParser.Parse(options["DEFAULT"]);
        Assert.That(onPrem.Path, Is.EqualTo(@"~\on-prem-funcs\"));
        Assert.That(onPrem.HasExclude, Is.False);
        Assert.That(onPrem.Sort, Is.Empty);
        Assert.That(onPrem.SortMarker, Is.EqualTo(DirectorySortMarker.Alpha));
        Assert.That(onPrem.SortMarkerPosition, Is.EqualTo(0));
    }

    [Test]
    public void Options_Can_Be_Bare_Paths()
    {
        var options = FlagDirectoryParser.Parse(@"(#Azure): azure-funcs (DEFAULT): D:\Scripts\On Prem Funcs");

        Assert.That(options["#AZURE"], Is.EqualTo("azure-funcs"));
        Assert.That(options["DEFAULT"], Is.EqualTo(@"D:\Scripts\On Prem Funcs"));
    }

    [TestCase(@"(DEFAULT): ~\funcs", "DEFAULT")]
    [TestCase(@"(#Azure): ~\funcs", "#AZURE")]
    [TestCase(@"   (#Azure):~\funcs   ", "#AZURE")]
    public void A_Single_Option_Is_Allowed(string input, string expectedKey)
    {
        var options = FlagDirectoryParser.Parse(input);

        Assert.That(options.Keys, Is.EqualTo(new[] { expectedKey }));
        Assert.That(options[expectedKey], Is.EqualTo(@"~\funcs"));
    }

    [Test]
    public void Keys_Are_Normalized_Case_Insensitive_And_The_Hash_Is_Optional()
    {
        var options = FlagDirectoryParser.Parse(@"(azure): ~\azure (default): ~\on-prem");

        Assert.That(options.Keys, Is.EqualTo(new[] { "#AZURE", "DEFAULT" }));
        Assert.That(options["#azure"], Is.EqualTo(@"~\azure"));
        Assert.That(options["Default"], Is.EqualTo(@"~\on-prem"));
    }

    [Test]
    public void Multiple_Keys_Can_Share_One_Option()
    {
        var options = FlagDirectoryParser.Parse(@"(#RDS, #Azure): ~\cloud [sort: ALPHADESC] (DEFAULT, #On_Prem-2): ~\on-prem");

        Assert.That(options.Keys, Is.EqualTo(new[] { "#RDS", "#AZURE", "DEFAULT", "#ON_PREM-2" }));
        Assert.That(options["#RDS"], Is.EqualTo(@"~\cloud [sort: ALPHADESC]"));
        Assert.That(options["#AZURE"], Is.EqualTo(@"~\cloud [sort: ALPHADESC]"));
        Assert.That(options["DEFAULT"], Is.EqualTo(@"~\on-prem"));
        Assert.That(options["#ON_PREM-2"], Is.EqualTo(@"~\on-prem"));
    }

    [Test]
    public void Allows_One_Space_Before_The_Colon()
    {
        var options = FlagDirectoryParser.Parse(@"( #Azure ) : ~\azure (DEFAULT) : ~\on-prem");

        Assert.That(options["#AZURE"], Is.EqualTo(@"~\azure"));
        Assert.That(options["DEFAULT"], Is.EqualTo(@"~\on-prem"));
    }

    [Test]
    public void Quoted_Paths_And_Entries_Can_Contain_Option_Like_Text()
    {
        var options = FlagDirectoryParser.Parse(@"(#Azure): ""~\funcs (DEFAULT): old"" [exclude: 'odd (#x): name.sql'] (DEFAULT): '~\on-prem (v2)'");

        Assert.That(options.Keys, Is.EqualTo(new[] { "#AZURE", "DEFAULT" }));
        Assert.That(options["#AZURE"], Is.EqualTo(@"""~\funcs (DEFAULT): old"" [exclude: 'odd (#x): name.sql']"));
        Assert.That(options["DEFAULT"], Is.EqualTo(@"'~\on-prem (v2)'"));

        Assert.That(DirectoryParser.Parse(options["#AZURE"]).Path, Is.EqualTo(@"~\funcs (DEFAULT): old"));
        Assert.That(DirectoryParser.Parse(options["#AZURE"]).Exclude, Is.EqualTo(new[] { "odd (#x): name.sql" }));
        Assert.That(DirectoryParser.Parse(options["DEFAULT"]).Path, Is.EqualTo(@"~\on-prem (v2)"));
    }
    #endregion

    #region Errors
    [Test]
    public void Null_Input_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FlagDirectoryParser.Parse(null!));
    }

    [TestCase("", "No (#FLAG) or (DEFAULT) options found")]
    [TestCase("   ", "No (#FLAG) or (DEFAULT) options found")]
    [TestCase(@": ~\on-prem-funcs", "Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-DIRECTORY")]
    [TestCase(@"~\on-prem-funcs (DEFAULT): ~\x", "Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-DIRECTORY")]
    [TestCase(@"(#Az ure): ~\x", "names may only contain")]
    [TestCase(@"(#Azure!): ~\x", "names may only contain")]
    [TestCase(@"(#Azure) ~\x", "Missing ':' after '(#Azure)'")]
    [TestCase(@"(#Azure)  : ~\x", "Missing ':' after '(#Azure)'")]
    [TestCase(@"(#Azure): ~\a (#azure): ~\b", "Duplicate option '#AZURE'")]
    [TestCase(@"(#RDS, #Azure): ~\a (DEFAULT, #RDS): ~\b", "Duplicate option '#RDS'")]
    public void Invalid_Options_Throw_With_A_Hint(string input, string expected)
    {
        var ex = Assert.Throws<FormatException>(() => FlagDirectoryParser.Parse(input));

        StringAssert.Contains(expected, ex!.Message);
    }

    [TestCase(@"(#Azure): (DEFAULT): ~\b", "(#Azure)", "A ##DIRECTORY path is required")]
    [TestCase(@"(#Azure): ~\a (DEFAULT):", "(DEFAULT)", "A ##DIRECTORY path is required")]
    [TestCase(@"(#Azure): ~\a [sort: ALPHA, TIMESTAMP] (DEFAULT): ~\b", "(#Azure)", "Only one sort marker")]
    [TestCase(@"(#Azure): ~\a [exlude: x.sql] (DEFAULT): ~\b", "(#Azure)", "Unknown clause '[exlude:'")]
    [TestCase(@"(#Azure): ~\a (DEFAULT): ~\b [sort: x.sql", "(DEFAULT)", "missing its closing ']'")]
    [TestCase(@"(#Azure): ~\a (DEFAULT): Reports (v2)", "(DEFAULT)", "must be wrapped in")]
    [TestCase(@"(#Azure): ~\a [exclude: notes.txt]", "(#Azure)", "[exclude:] entries must be .sql files")]
    public void Invalid_Directory_Details_Throw_With_The_Option_That_Has_Them(string input, string option, string expected)
    {
        var ex = Assert.Throws<FormatException>(() => FlagDirectoryParser.Parse(input));

        StringAssert.Contains($"Invalid ##DIRECTORY details for {option}:", ex!.Message);
        StringAssert.Contains(expected, ex.Message);
    }
    #endregion
}
