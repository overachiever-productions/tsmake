namespace tsmake.tests.unit_tests;

[TestFixture]
public class HeaderCommentsTests
{
    private static string Text(params string[] lines) => string.Join("\r\n", lines);

    // The regex is anchored at \A - so Replace() removes (at most) the one header.
    private static string RemoveHeader(string text) => Global.HeaderCommentsRegex.Replace(text, string.Empty);

    private static void AssertNoHeader(string text)
    {
        Assert.That(Global.HeaderCommentsRegex.IsMatch(text), Is.False);
        Assert.That(RemoveHeader(text), Is.EqualTo(text));
    }

    #region Business-rule examples (as specified)
    [Test]
    public void Header_Ends_Where_Code_Starts()
    {
        var text = Text(
            "-- this is part of header - as we haven't hit non-comment content yet. ",
            "/* this is part of the ",
            "comment ",
            "too */",
            "-- but everything after this line is non-HEADER comment because next line = T-SQL",
            "USE [myDatabase];",
            "GO ",
            "",
            "-- this is a comment. it's valid within T-SQL syntax. But it's NOT a tsmake \"header comment\".");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "USE [myDatabase];",
            "GO ",
            "",
            "-- this is a comment. it's valid within T-SQL syntax. But it's NOT a tsmake \"header comment\".")));
    }

    [Test]
    public void Block_Comments_After_Code_Are_Not_Header_Comments()
    {
        var text = Text(
            "/* this is a header comment */",
            "/* and so is this. ",
            "But anything AFTER this block-comment is NON-HEADER in scope */",
            "IF OBJECT_ID(N'dbo.myTable', N'U') IS NOT NULL BEGIN ",
            "     PRINT N'xxxx found';",
            "END;",
            "",
            "/* this comment is fine, but it appears AFTER \"code\" - so it's not a header comment */",
            "",
            "USE [someDatabase]; -- also a valid T-SQL comment, but not a tsmake HEADER comment");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "IF OBJECT_ID(N'dbo.myTable', N'U') IS NOT NULL BEGIN ",
            "     PRINT N'xxxx found';",
            "END;",
            "",
            "/* this comment is fine, but it appears AFTER \"code\" - so it's not a header comment */",
            "",
            "USE [someDatabase]; -- also a valid T-SQL comment, but not a tsmake HEADER comment")));
    }

    [Test]
    public void Files_Without_Header_Comments_Are_Not_Altered()
    {
        AssertNoHeader(Text(
            "SELECT N'no header comment found in this file' [status];",
            "",
            "PRINT N'other code and such would go here... ';"));
    }

    [Test]
    public void Whitespace_Within_The_Code_Line_Is_Preserved()
    {
        var text = Text(
            "      -- this is a comment (preceded by white-space)",
            "     SELECT N'this is the line where \"CODE\" starts';");

        Assert.That(RemoveHeader(text), Is.EqualTo("     SELECT N'this is the line where \"CODE\" starts';"));
    }

    [Test]
    public void A_Multi_Line_Block_Comment_Is_Removed_Up_To_The_Code_On_Its_Last_Line()
    {
        var text = Text(
            "/* this case is a BIT different than the above - because of the way that a T-SQL block-comment can span MULTIPLE lines and/or PARTS of",
            "   lines */USE [master];",
            "GO ",
            "",
            "-- more code here, etc. ");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "USE [master];",
            "GO ",
            "",
            "-- more code here, etc. ")));
    }

    [Test]
    public void Whitespace_Before_Header_Comments_Is_Part_Of_The_Header()
    {
        var text = Text(
            "",
            "",
            "-- this is a header comment. ",
            "PRINT N'this is code';");

        Assert.That(RemoveHeader(text), Is.EqualTo("PRINT N'this is code';"));
    }

    [Test]
    public void A_Block_Comment_On_The_Code_Line_Is_Removed()
    {
        var text = Text(
            "/* block comment here */USE [admindb];",
            "GO ",
            "",
            "IF OBJECT_ID(.... etc");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "USE [admindb];",
            "GO ",
            "",
            "IF OBJECT_ID(.... etc")));
    }

    [Test]
    public void Lots_Of_Comments_Nested_Comments_And_Whitespace_Are_All_Header()
    {
        var text = Text(
            "     ",
            "--- comment",
            "/* comment ",
            "   /* nested comment",
            "    that is fine/well-formed */",
            "*/",
            "",
            "/* yet another block comment */USE [master];",
            "GO ",
            "",
            "BEGIN TRAN ",
            "   -- etc... ",
            "");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "USE [master];",
            "GO ",
            "",
            "BEGIN TRAN ",
            "   -- etc... ",
            "")));
    }

    [Test]
    public void Leading_Whitespace_Is_Kept_When_Code_Comes_Before_Any_Comment()
    {
        AssertNoHeader(Text(
            "",
            "USE [master];  -- this isn't a header comment. ",
            "GO"));
    }

    [Test]
    public void Commented_Out_Code_Is_Header_Comment()
    {
        var text = Text(
            "",
            "--USE [master]; -- this isn't a header comment (but it actually is)",
            "GO",
            "",
            "-- other code here... etc. ");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text(
            "GO",
            "",
            "-- other code here... etc. ")));
    }
    #endregion

    #region What is (and isn't) a header
    [TestCase("SELECT 1;")]
    [TestCase("\r\n\r\nSELECT 1; -- trailing comment")]
    [TestCase("   \t SELECT 1; /* trailing comment */")]
    [TestCase("SELECT 1 /* inline */;\r\n-- comment")]
    [TestCase("PRINT '-- not a comment';")]
    [TestCase("PRINT N'/* not a comment */';")]
    [TestCase("[-- not a comment] = 1;")]
    [TestCase("-1;")]
    [TestCase("/ 2;")]
    [TestCase("")]
    [TestCase("   \r\n\t\r\n  ")]                       // whitespace alone is NOT a header.
    public void There_Is_No_Header_Unless_A_Comment_Comes_Before_Any_Code(string text)
    {
        AssertNoHeader(text);
    }

    [TestCase("-- one\r\nSELECT 1;")]
    [TestCase("-- one\r\n-- two\r\n--three\r\nSELECT 1;")]
    [TestCase("/* one */\r\nSELECT 1;")]
    [TestCase("/* one */ /* two */\r\n/* three */\r\nSELECT 1;")]
    [TestCase("/* one */ -- two\r\n-- three /* not a block */\r\n/* four -- not a line comment */\r\nSELECT 1;")]
    [TestCase("-- one\r\n\r\n\t\r\n/* two\r\n\r\n */\r\n   \r\n-- three\r\nSELECT 1;")]
    [TestCase("---------------\r\n-- banner\r\n---------------\r\nSELECT 1;")]
    public void Header_Comments_Can_Be_Any_Combination_Of_Comments_And_Whitespace(string text)
    {
        Assert.That(RemoveHeader(text), Is.EqualTo("SELECT 1;"));
    }

    [Test]
    public void Comments_After_Code_Are_Never_Header_Comments()
    {
        var text = Text("-- header", "SELECT 1;", "-- not header", "/* not header */", "SELECT 2;");

        Assert.That(RemoveHeader(text), Is.EqualTo(Text("SELECT 1;", "-- not header", "/* not header */", "SELECT 2;")));
    }

    [TestCase("-- see /* this does not open a block comment\r\nSELECT 1;")]
    [TestCase("-- odd */ text\r\nSELECT 1;")]
    [TestCase("/* -- this does not hide the end of the block */SELECT 1;")]
    [TestCase("/* a */ -- b\r\nSELECT 1;")]
    public void Comment_Markers_Within_Comments_Are_Just_Comment_Text(string text)
    {
        Assert.That(RemoveHeader(text), Is.EqualTo("SELECT 1;"));
    }

    [TestCase("/* a /* b /* c */ b */ a */\r\nSELECT 1;")]
    [TestCase("/* outer\r\n  /* inner */ still outer\r\n*/SELECT 1;")]
    [TestCase("/**/ /***/ /* ** */\r\nSELECT 1;")]
    [TestCase("/* /* nested */ */ -- and a line comment\r\nSELECT 1;")]
    public void Nested_Block_Comments_Are_Header_Comments(string text)
    {
        Assert.That(RemoveHeader(text), Is.EqualTo("SELECT 1;"));
    }

    [TestCase("-- comment\r\n\t  SELECT 1;", "\t  SELECT 1;")]                         // whole lines removed: indentation kept.
    [TestCase("/* comment */\r\n    SELECT 1;", "    SELECT 1;")]
    [TestCase("/* comment */ \t SELECT 1;", "SELECT 1;")]                               // block comment ON the code line: removed up to the code.
    [TestCase("   /* a */ /* b */  SELECT 1;", "SELECT 1;")]
    [TestCase("/* a\r\n */   SELECT 1;\r\n  SELECT 2;", "SELECT 1;\r\n  SELECT 2;")]
    public void Header_Removal_Keeps_The_Code_Line_Intact(string text, string expected)
    {
        Assert.That(RemoveHeader(text), Is.EqualTo(expected));
    }

    [TestCase("-- 6.0\r\n-- 6.1\r\n/* 6.2 */")]
    [TestCase("-- 6.0\r\n-- 6.1\r\n/* 6.2 */\r\n")]
    [TestCase("\r\n  /* just a comment */  \r\n\r\n")]
    [TestCase("--")]
    public void A_File_Of_Only_Comments_And_Whitespace_Is_All_Header(string text)
    {
        var header = Global.HeaderCommentsRegex.Match(text);

        Assert.That(header.Success, Is.True);
        Assert.That(header.Length, Is.EqualTo(text.Length));
        Assert.That(RemoveHeader(text), Is.Empty);
    }

    [Test]
    public void An_Unclosed_Block_Comment_Is_Not_A_Header_Comment()
    {
        // (The Normalizer reports these as syntax errors.) Only VALID comments before it are header.
        AssertNoHeader("/* unclosed\r\nSELECT 1;");
        AssertNoHeader("/* outer /* inner */\r\nSELECT 1;");
        Assert.That(RemoveHeader("-- ok\r\n/* unclosed\r\nSELECT 1;"), Is.EqualTo("/* unclosed\r\nSELECT 1;"));
    }

    [TestCase("\n")]
    [TestCase("\r")]
    [TestCase("\r\n")]
    public void Works_With_Any_Line_Endings(string lineEnding)
    {
        string text = string.Join(lineEnding, "", "-- header", "/* header", "   header */", "   SELECT 1;", "-- not header");

        Assert.That(RemoveHeader(text), Is.EqualTo(string.Join(lineEnding, "   SELECT 1;", "-- not header")));
    }
    #endregion

    #region tsmake directives ARE header comments (as designed - for now)
    // Directives are -- comments: so a directive BEFORE the first code is part of the header - and is removed with it.
    [TestCase("-- ##FILE: x.sql")]
    [TestCase("--##FILE: x.sql")]
    [TestCase("   -- ##DIRECTORY: tables [sort: ALPHA]")]
    [TestCase("-- ##FLAG-FILE(#Azure): a.sql (DEFAULT): b.sql")]
    [TestCase("-- ##FLAG-DIRECTORY(#RDS, #Azure): ~\\cloud (DEFAULT): ~\\on-prem")]
    [TestCase("-- ##OUTPUT: ~\\Deployment\\latest.sql")]
    [TestCase("-- ##ROOT: ~\\")]
    [TestCase("-- ##COMMENT: a tsmake comment")]
    [TestCase("-- ##:: a tsmake comment")]
    [TestCase("-- ##CONDITION-DEFAULT: x")]
    [TestCase("-- ##file: x.sql")]
    public void Directives_Before_The_First_Code_Are_Header_Comments(string directive)
    {
        // (A real directive - per the DirectiveProcessor's rules.)
        Assert.That(Global.DirectiveRegex.IsMatch(directive), Is.True);

        Assert.That(RemoveHeader(Text(directive, "-- comment", "SELECT 1;")), Is.EqualTo("SELECT 1;"));
        Assert.That(RemoveHeader(Text("/* header */", directive, "-- comment", "SELECT 1;")), Is.EqualTo("SELECT 1;"));
        Assert.That(RemoveHeader(Text("-- header", directive)), Is.Empty);
    }

    [TestCase("-- ##FILE: x.sql")]
    [TestCase("-- ##DIRECTORY: tables")]
    public void Directives_After_The_First_Code_Are_Not_Header_Comments(string directive)
    {
        Assert.That(RemoveHeader(Text("-- header", "SELECT 1;", directive, "SELECT 2;")), Is.EqualTo(Text("SELECT 1;", directive, "SELECT 2;")));
    }

    [TestCase("-- ##TODO: not a known directive")]
    [TestCase("-- ## FILE: space after the ##")]
    [TestCase("/* x */ -- ##FILE: not at the start of the line")]
    [TestCase("/* -- ##FILE: inside a block comment */")]
    [TestCase("/*\r\n-- ##FILE: inside a (multi-line) block comment\r\n*/")]
    public void Directive_Like_Comments_Are_Header_Comments(string comment)
    {
        Assert.That(RemoveHeader(Text(comment, "SELECT 1;")), Is.EqualTo("SELECT 1;"));
    }
    #endregion
}
