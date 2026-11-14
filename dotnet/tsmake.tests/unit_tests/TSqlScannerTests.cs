namespace tsmake.tests.unit_tests;

// NOTE: the scanner's lexing rules are ALSO covered (extensively) via NormalizerTests' closure validations.
[TestFixture]
public class TSqlScannerTests
{
    private static List<(int Index, SqlRegion Region)> Scan(TSqlScanner scanner, string line, List<int>? strayCommentEnds = null)
    {
        var regions = new List<(int, SqlRegion)>();
        scanner.ScanLine(line, (index, region) => regions.Add((index, region)), index => strayCommentEnds?.Add(index));
        return regions;
    }

    [Test]
    public void It_Reports_Where_Each_Region_Starts()
    {
        var scanner = new TSqlScanner();

        //                          0         1         2         3
        //                          0123456789012345678901234567890123
        var regions = Scan(scanner, "SELECT 'a', [b], \"c\" /* d */ -- e");

        Assert.That(regions, Is.EqualTo(new[] {
            (7, SqlRegion.String), (10, SqlRegion.Code),
            (12, SqlRegion.BracketedIdentifier), (15, SqlRegion.Code),
            (17, SqlRegion.QuotedIdentifier), (20, SqlRegion.Code),
            (21, SqlRegion.BlockComment), (28, SqlRegion.Code),
            (29, SqlRegion.EolComment) }));
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.Code));        // a -- comment ends with its line.
    }

    [Test]
    public void Doubled_Delimiters_Do_Not_Close_Their_Region()
    {
        //                                       0         1         2
        //                                       012345678901234567890
        var regions = Scan(new TSqlScanner(), "'it''s' [a]]b] \"c\"\"d\"");

        Assert.That(regions, Is.EqualTo(new[] {
            (0, SqlRegion.String), (7, SqlRegion.Code),
            (8, SqlRegion.BracketedIdentifier), (14, SqlRegion.Code),
            (15, SqlRegion.QuotedIdentifier), (21, SqlRegion.Code) }));
    }

    [Test]
    public void Strings_Comments_And_Identifiers_Carry_On_To_The_Next_Line()
    {
        var scanner = new TSqlScanner();

        Scan(scanner, "PRINT 'line one");
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.String));

        Assert.That(Scan(scanner, "line two' + 'x';"), Is.EqualTo(new[] { (9, SqlRegion.Code), (12, SqlRegion.String), (15, SqlRegion.Code) }));
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.Code));

        Scan(scanner, "/* a block");
        Scan(scanner, "   comment -- with ' and [ in it");
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.BlockComment));
        Assert.That(Scan(scanner, "*/ SELECT [multi"), Is.EqualTo(new[] { (2, SqlRegion.Code), (10, SqlRegion.BracketedIdentifier) }));
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.BracketedIdentifier));
    }

    [Test]
    public void Block_Comments_Can_Be_Nested()
    {
        var scanner = new TSqlScanner();

        //                                  0         1         2
        //                                  0123456789012345678901234567890
        Assert.That(Scan(scanner, "/* a /* b */ still comment */ SELECT"), Is.EqualTo(new[] { (0, SqlRegion.BlockComment), (29, SqlRegion.Code) }));
        Assert.That(scanner.Region, Is.EqualTo(SqlRegion.Code));
    }

    [Test]
    public void It_Reports_Block_Comment_Ends_Found_In_Code()
    {
        var strays = new List<int>();

        //                                         0         1         2
        //                                         01234567890123456789012
        var regions = Scan(new TSqlScanner(), "SELECT 1; */ t.*/* x */", strays);

        Assert.That(strays, Is.EqualTo(new[] { 10 }));                  // NOT the * in "t.*/*" - its / starts a comment.
        Assert.That(regions, Is.EqualTo(new[] { (16, SqlRegion.BlockComment), (23, SqlRegion.Code) }));
    }
}
