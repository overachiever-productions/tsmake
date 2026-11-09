namespace tsmake.tests.unit_tests;

[TestFixture]
public class PrioritizeAndExcludeTests
{
    private const string Dir = @"D:\Repositories\myProject\includes\alpha_sixes";

    // Files a.sql ... {last}.sql - with timestamps deliberately NOT in alphabetical order (newest first).
    private static List<(string FileName, DateTime TimeStamp)> Files(char last, string prefix = "")
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0);
        int count = last - 'a' + 1;

        return Enumerable.Range(0, count)
            .Select(i => ($@"{Dir}\{prefix}{(char)('a' + i)}.sql", start.AddMinutes(count - i)))
            .ToList();
    }

    // Specific file names (in the order given) - timestamps newest first, same as Files().
    private static List<(string FileName, DateTime TimeStamp)> Named(params string[] names)
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0);

        return names
            .Select((name, i) => ($@"{Dir}\{name}", start.AddMinutes(names.Length - i)))
            .ToList();
    }

    private static List<string> Run(List<(string FileName, DateTime TimeStamp)> files, string clauses)
    {
        var options = DirectoryParser.Parse($"alpha_sixes {clauses}");
        return files.PrioritizeAndExclude(options).Select(f => Path.GetFileName(f)).ToList();
    }

    [Test]
    public void Nester_Directive_Pins_First_And_Last_Around_Alpha()
    {
        // test_files\includes\nester.sql:  ##DIRECTORY: alpha_sixes [exclude: 6e.sql] [sort: 6d.sql, ALPHA, 6a.sql]
        var result = Run(Files('e', prefix: "6"), "[exclude: 6e.sql] [sort: 6d.sql, ALPHA, 6a.sql]");

        Assert.That(result, Is.EqualTo(new[] { "6d.sql", "6b.sql", "6c.sql", "6a.sql" }));
    }

    #region [exclude:] - exact file names
    [Test]
    public void Exclude_Matches_Exact_File_Names_Not_Substrings()
    {
        var files = Named("a.sql", "e.sql", "6e.sql", "table.sql", "type.sql", "e.sql.sql");

        var result = Run(files, "[exclude: e.sql]");

        Assert.That(result, Is.EqualTo(new[] { "6e.sql", "a.sql", "e.sql.sql", "table.sql", "type.sql" }));
    }

    [Test]
    public void Exclude_Is_Case_Insensitive()
    {
        var result = Run(Files('d'), "[exclude: B.SQL, 'C.sql']");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "d.sql" }));
    }

    [Test]
    public void Exclude_Handles_Quoted_Names_With_Spaces_Commas_And_Parens()
    {
        var files = Named("a.sql", "my file.sql", "old, unused.sql", "v(2).sql", "z.sql");

        var result = Run(files, "[exclude: my file.sql, \"old, unused.sql\", 'v(2).sql']");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "z.sql" }));
    }

    [Test]
    public void Exclude_Matches_A_Full_Path()
    {
        var result = Run(Files('c'), $"[exclude: \"{Dir}\\b.sql\"]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "c.sql" }));
    }

    [Test]
    public void Exclude_Entries_For_Missing_Files_Are_Ignored()
    {
        var result = Run(Files('c'), "[exclude: zz.sql, b.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "c.sql" }));
    }

    [Test]
    public void Exclude_Does_Not_Match_On_Directory_Names()
    {
        // The directory portion of the path must never be considered (e.g., a folder named 'a.sql').
        var files = new List<(string FileName, DateTime TimeStamp)>
        {
            (@"D:\Repositories\a.sql\b.sql", DateTime.Now),
            (@"D:\Repositories\a.sql\c.sql", DateTime.Now),
        };

        var result = Run(files, "[exclude: a.sql]");

        Assert.That(result, Is.EqualTo(new[] { "b.sql", "c.sql" }));
    }
    #endregion

    [Test]
    public void Multiple_Pins_Before_And_After_Marker_Keep_Their_Literal_Order()
    {
        var result = Run(Files('m'), "[sort: m.sql, l.sql, ALPHA, a.sql, b.sql]");

        Assert.That(result, Is.EqualTo(new[]
        {
            "m.sql", "l.sql",
            "c.sql", "d.sql", "e.sql", "f.sql", "g.sql", "h.sql", "i.sql", "j.sql", "k.sql",
            "a.sql", "b.sql"
        }));
    }

    [Test]
    public void AlphaDesc_Sorts_Unpinned_Files_Descending()
    {
        var result = Run(Files('f'), "[sort: c.sql, ALPHADESC, d.sql]");

        Assert.That(result, Is.EqualTo(new[] { "c.sql", "f.sql", "e.sql", "b.sql", "a.sql", "d.sql" }));
    }

    [Test]
    public void Timestamp_Sorts_Unpinned_Files_Oldest_First()
    {
        // Files() makes a.sql the NEWEST and f.sql the OLDEST.
        var result = Run(Files('f'), "[sort: TIMESTAMP, a.sql]");

        Assert.That(result, Is.EqualTo(new[] { "f.sql", "e.sql", "d.sql", "c.sql", "b.sql", "a.sql" }));
    }

    [Test]
    public void TimestampDesc_Sorts_Unpinned_Files_Newest_First()
    {
        var result = Run(Files('f'), "[sort: f.sql, TimestampDESC]");

        Assert.That(result, Is.EqualTo(new[] { "f.sql", "a.sql", "b.sql", "c.sql", "d.sql", "e.sql" }));
    }

    [Test]
    public void Marker_Only_Sort_Pins_Nothing()
    {
        var result = Run(Files('d'), "[sort: ALPHADESC]");

        Assert.That(result, Is.EqualTo(new[] { "d.sql", "c.sql", "b.sql", "a.sql" }));
    }

    [Test]
    public void No_Sort_Clause_Defaults_To_Alpha()
    {
        var files = Files('d');
        files.Reverse();

        var result = Run(files, "");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql", "c.sql", "d.sql" }));
    }

    [Test]
    public void Names_Without_A_Marker_Are_Pinned_First_Then_Alpha()
    {
        var result = Run(Files('e'), "[sort: d.sql, b.sql]");

        Assert.That(result, Is.EqualTo(new[] { "d.sql", "b.sql", "a.sql", "c.sql", "e.sql" }));
    }

    [Test]
    public void Excluded_File_Is_Not_Pinned_Back_In()
    {
        var result = Run(Files('d'), "[exclude: a.sql] [sort: ALPHA, a.sql]");

        Assert.That(result, Is.EqualTo(new[] { "b.sql", "c.sql", "d.sql" }));
    }

    [Test]
    public void Pins_For_Missing_Files_Are_Ignored()
    {
        var result = Run(Files('c'), "[sort: zz.sql, ALPHA, yy.sql, a.sql]");

        Assert.That(result, Is.EqualTo(new[] { "b.sql", "c.sql", "a.sql" }));
    }

    #region Wildcards - [exclude:]
    [Test]
    public void Exclude_Star_Matches_Zero_Or_More_Characters()
    {
        var files = Named("a.sql", "test_.sql", "test_1.sql", "test_22.sql", "testing.sql", "b.sql");

        var result = Run(files, "[exclude: test_*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql", "testing.sql" }));
    }

    [Test]
    public void Exclude_Question_Mark_Matches_Exactly_One_Character()
    {
        var files = Named("0.sql", "01.sql", "02.sql", "010.sql", "1a.sql");

        var result = Run(files, "[exclude: 0?.sql]");

        Assert.That(result, Is.EqualTo(new[] { "0.sql", "010.sql", "1a.sql" }));
    }

    [Test]
    public void Exclude_Wildcards_Are_Case_Insensitive()
    {
        var files = Named("Test_1.sql", "TEST_2.SQL", "test_3.sql", "keep.sql");

        var result = Run(files, "[exclude: test_*.SQL]");

        Assert.That(result, Is.EqualTo(new[] { "keep.sql" }));
    }

    [Test]
    public void Exclude_Wildcards_Can_Be_Combined_With_Exact_Names()
    {
        var files = Named("a.sql", "b.sql", "old_1.sql", "old_2.sql", "z.sql");

        var result = Run(files, "[exclude: old_*.sql, z.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql" }));
    }

    [Test]
    public void Exclude_Wildcards_Treat_Other_Characters_Literally()
    {
        // '.', '(', ')', '+' etc. must NOT behave like regex operators.
        var files = Named("v(1)_x.sql", "v1_x.sql", "a+b.sql", "aab.sql", "a.b1.sql", "axb1.sql");

        var result = Run(files, "[exclude: 'v(1)*.sql', a+?.sql, a.b*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "aab.sql", "axb1.sql", "v1_x.sql" }));
    }

    [Test]
    public void Exclude_Wildcards_Do_Not_Match_On_Directory_Names()
    {
        var files = new List<(string FileName, DateTime TimeStamp)>
        {
            (@"D:\Repositories\test_dir\a.sql", DateTime.Now),
            (@"D:\Repositories\test_dir\test_b.sql", DateTime.Now),
        };

        var result = Run(files, "[exclude: *test*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql" }));
    }

    [Test]
    public void Exclude_Wildcard_With_A_Path_Separator_Matches_The_Full_Path()
    {
        var result = Run(Files('d'), $"[exclude: \"{Dir}\\[bc]*.sql\", \"{Dir}\\d*.sql\"]");

        // '[bc]' is literal (NOT a character class) - so only d.sql is excluded.
        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql", "c.sql" }));
    }

    [Test]
    public void Exclude_Star_Sql_Excludes_Everything()
    {
        var result = Run(Files('c'), "[exclude: *.sql] [sort: a.sql, ALPHA]");

        Assert.That(result, Is.Empty);
    }
    #endregion

    #region Wildcards - [sort:]
    [Test]
    public void Wildcard_Pin_Before_Marker_Pulls_All_Matches_To_The_Front()
    {
        var files = Named("a.sql", "z_2.sql", "b.sql", "z_1.sql", "z_10.sql");

        var result = Run(files, "[sort: z_*.sql, ALPHA]");

        Assert.That(result, Is.EqualTo(new[] { "z_1.sql", "z_10.sql", "z_2.sql", "a.sql", "b.sql" }));
    }

    [Test]
    public void Wildcard_Pin_After_Marker_Pulls_All_Matches_To_The_End()
    {
        var files = Named("x_1.sql", "a.sql", "x_2.sql", "b.sql");

        var result = Run(files, "[sort: ALPHA, x_*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql", "x_1.sql", "x_2.sql" }));
    }

    [Test]
    public void Wildcard_Pin_Matches_Are_Ordered_By_The_Sort_Marker()
    {
        var files = Named("a.sql", "b.sql", "x_1.sql", "x_2.sql", "x_3.sql");

        var result = Run(files, "[sort: ALPHADESC, x_*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "b.sql", "a.sql", "x_3.sql", "x_2.sql", "x_1.sql" }));
    }

    [Test]
    public void Wildcard_Pin_Matches_Follow_Timestamp_Markers()
    {
        // Named() gives the FIRST name the NEWEST timestamp: x_3 is newest, x_1 oldest.
        var files = Named("x_3.sql", "x_2.sql", "x_1.sql", "b.sql", "a.sql");

        var oldestFirst = Run(files, "[sort: x_*.sql, TIMESTAMP]");
        var newestFirst = Run(files, "[sort: x_*.sql, TIMESTAMPDESC]");

        Assert.That(oldestFirst, Is.EqualTo(new[] { "x_1.sql", "x_2.sql", "x_3.sql", "a.sql", "b.sql" }));
        Assert.That(newestFirst, Is.EqualTo(new[] { "x_3.sql", "x_2.sql", "x_1.sql", "b.sql", "a.sql" }));
    }

    [Test]
    public void Wildcard_Pin_Without_A_Marker_Is_Pinned_First_And_Alpha_Ordered()
    {
        var files = Named("a.sql", "z_2.sql", "z_1.sql");

        var result = Run(files, "[sort: z_*.sql]");

        Assert.That(result, Is.EqualTo(new[] { "z_1.sql", "z_2.sql", "a.sql" }));
    }

    [Test]
    public void First_Matching_Entry_Wins()
    {
        // 01*.sql claims 01.sql and 011.sql BEFORE 0*.sql gets a chance at them.
        var files = Named("00.sql", "01.sql", "011.sql", "02.sql", "a.sql");

        var specificFirst = Run(files, "[sort: 01*.sql, 0*.sql, ALPHA]");
        var generalFirst = Run(files, "[sort: 0*.sql, 01*.sql, ALPHA]");

        Assert.That(specificFirst, Is.EqualTo(new[] { "01.sql", "011.sql", "00.sql", "02.sql", "a.sql" }));
        Assert.That(generalFirst, Is.EqualTo(new[] { "00.sql", "01.sql", "011.sql", "02.sql", "a.sql" }));
    }

    [Test]
    public void Wildcard_And_Exact_Pins_Keep_Their_Literal_Order()
    {
        var files = Named("a.sql", "b.sql", "c.sql", "x_1.sql", "x_2.sql", "y.sql");

        var result = Run(files, "[sort: b.sql, x_*.sql, ALPHA, ?.sql]");

        // '?.sql' (after the marker) claims the remaining single-character names: a, c, y.
        Assert.That(result, Is.EqualTo(new[] { "b.sql", "x_1.sql", "x_2.sql", "a.sql", "c.sql", "y.sql" }));
    }

    [Test]
    public void Wildcard_Pin_Does_Not_Pull_Back_Excluded_Files()
    {
        var files = Named("a.sql", "x_1.sql", "x_2.sql", "x_3.sql");

        var result = Run(files, "[exclude: x_2.sql] [sort: x_*.sql, ALPHA]");

        Assert.That(result, Is.EqualTo(new[] { "x_1.sql", "x_3.sql", "a.sql" }));
    }

    [Test]
    public void Wildcard_Pin_With_No_Matches_Is_Ignored()
    {
        var result = Run(Files('c'), "[sort: zz_*.sql, ALPHA, q?.sql]");

        Assert.That(result, Is.EqualTo(new[] { "a.sql", "b.sql", "c.sql" }));
    }
    #endregion
}
