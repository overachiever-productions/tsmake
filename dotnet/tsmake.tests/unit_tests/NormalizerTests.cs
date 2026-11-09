namespace tsmake.tests.unit_tests.normalizer;

// TODO: 
//     need to add a whole SUITE of tests that verify that ArtifactLines passed IN to a normalizer are preserved and not modified.
//          and that NEW lines are simply ADDED to what was already there. 

// TODO: verify that we're getting syntax errors when/as applicable as well. 
//   including closures-errors, etc. 

public class NormalizerTests
{
    #region Core Normalization Tests
    [Test]
    public void It_Does_Not_Split_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Does_Not_Split_Empty_Strings()
    {
        var text = string.Empty;
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(string.Empty, sut.NormalizedText);
    }

    [Test]
    public void It_Does_Not_Add_LineEndings_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_CrLf()
    {
        var text = "first-line\r\nsecond-line\r\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Lf()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Lf_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Cr()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Cr_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Normalizes_To_Specified_LineEndingOptions()
    {
        var text = "first\r\nsecond\r\nthird\r\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer(LineEndingsType.Cr);
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\rsecond\rthird\rfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Mixed_Line_Endings()
    {
        var text = "first\r\nsecond\rthird\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\r\nsecond\r\nthird\r\nfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Keeps_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\r\nline2\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Normalized_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\rline2\r";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("line1\r\nline2\r\n", sut.NormalizedText);
    }

    [Test]
    public void It_Calibrates_Line_Endings_Correctly()
    {
        var text = "1\r\n22\r\n333\r\n4444\r\n\r\n666666\r\n7777777\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(lines.Count, Is.EqualTo(8));

        StringAssert.AreEqualIgnoringCase("1", lines[0].OriginalContent);
        StringAssert.AreEqualIgnoringCase("22", lines[1].OriginalContent);
        StringAssert.AreEqualIgnoringCase("333", lines[2].OriginalContent);
        StringAssert.AreEqualIgnoringCase("4444", lines[3].OriginalContent);
        StringAssert.AreEqualIgnoringCase("", lines[4].OriginalContent);
        StringAssert.AreEqualIgnoringCase("666666", lines[5].OriginalContent);
        StringAssert.AreEqualIgnoringCase("7777777", lines[6].OriginalContent);
        StringAssert.AreEqualIgnoringCase("", lines[7].OriginalContent);

        Assert.That(lines[0].LineNumber, Is.EqualTo(1));
        Assert.That(lines[1].LineNumber, Is.EqualTo(2));
        Assert.That(lines[2].LineNumber, Is.EqualTo(3));
        Assert.That(lines[3].LineNumber, Is.EqualTo(4));
        Assert.That(lines[4].LineNumber, Is.EqualTo(5));
        Assert.That(lines[5].LineNumber, Is.EqualTo(6));
        Assert.That(lines[6].LineNumber, Is.EqualTo(7));
        Assert.That(lines[7].LineNumber, Is.EqualTo(8));

        Assert.That(lines[0].OriginalContent.Length, Is.EqualTo(1));
        Assert.That(lines[1].OriginalContent.Length, Is.EqualTo(2));
        Assert.That(lines[2].OriginalContent.Length, Is.EqualTo(3));
        Assert.That(lines[3].OriginalContent.Length, Is.EqualTo(4));
        Assert.That(lines[4].OriginalContent.Length, Is.EqualTo(0));
        Assert.That(lines[5].OriginalContent.Length, Is.EqualTo(6));
        Assert.That(lines[6].OriginalContent.Length, Is.EqualTo(7));
        Assert.That(lines[7].OriginalContent.Length, Is.EqualTo(0));
    }
    #endregion

    #region Closure Validations
    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Strings()
    {
        var text = "PRINT 'Hello World!; -- note the missing end-tick... ";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated String.", errors[0].Message);
    }

    [Test]
    public void It_Does_Not_Have_Syntax_Errors_For_Correctly_Formed_Strings()
    {
        // NOTE: All of the following are correctly formed: 
        var text = "DECLARE @simple sysname = N'this is simple';\r\nDECLARE @complex sysname = N'this is complex with a comment /* and a string '' and an unclosed string '' and an unclosed comment /*';\r\nDECLARE @multiline sysname = N'this spans\r\nmultiple\r\nlines';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Correctly_Identifies_Location_Of_Unclosed_Strings()
    {
        var text = "DECLARE @simple sysname = N'this is simple';\r\nDECLARE @complex sysname = N'this is complex with a comment /* and a string '' and an unclosed string '' and an unclosed comment /*';\r\nDECLARE @multiline sysname = N'this spans\r\nmultiple\r\nlines';\r\nDECLARE @butThisisBad sysname = N'total fail\r\n-- closing comment. ";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated String.", errors[0].Message);

        Assert.That(errors[0].FileName, Is.EqualTo("file-name.sql"));
        Assert.That(errors[0].LineNumber, Is.EqualTo(6));

        StringAssert.Contains("total fail", errors[0].Detail);
    }

    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Block_Comments()
    {
        var text = "/* This is an unclosed block comment\r\nPRINT 'Hello World!';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated Block-Comment", errors[0].Message);
        Assert.That(errors[0].LineNumber, Is.EqualTo(1));       // the /* is at column 0 - which used to be reported as line -99.
    }

    [Test]
    public void It_Ignores_Unclosed_BlockComments_Within_Strings()
    {
        var text = "DECLARE @anotherString nvarchar(max) = N'this is not an unclosed block comment /* ';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_BracketIdentifiers()
    {
        var text = "SELECT * FROM [MyTable;\r\nGO";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated", errors[0].Message);
    }

    [Test]
    public void It_Ignores_Unclosed_BracketIdentifiers_Within_Strings()
    {
        var text = "DECLARE @mytext nvarchar(MAX) = N'this is not [a real identifier';\r\nGO";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);

        Assert.That(errors.Count, Is.EqualTo(0));
    }

    private static List<ISyntaxError> ClosureErrors(string text, LineEndingsType lineEndings = LineEndingsType.CrLf)
    {
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();

        new Normalizer(lineEndings).Normalize(text, lines, errors, new FileDetail("file-name.sql", 0, 0));
        return errors;
    }

    private static void AssertSingleError(List<ISyntaxError> errors, string message, int lineNumber, string? detail = null)
    {
        Assert.That(errors.Select(e => $"line {e.LineNumber}: {e.Message} [{e.Detail}]").ToList(), Has.Count.EqualTo(1));
        Assert.That(errors[0].Message, Is.EqualTo(message));
        Assert.That(errors[0].LineNumber, Is.EqualTo(lineNumber));
        if (detail != null)
            Assert.That(errors[0].Detail, Is.EqualTo(detail));
    }

    private static void AssertNoErrors(List<ISyntaxError> errors) =>
        Assert.That(errors.Select(e => $"line {e.LineNumber}: {e.Message} [{e.Detail}]"), Is.Empty);

    // -- comments: ticks, /*, [ and " within them are just comment text.
    [TestCase("SELECT 1; -- it's fine\r\nSELECT 2;")]
    [TestCase("-- don't\r\nSELECT 'abc';")]
    [TestCase("-- don't\r\nSELECT 'a [b' AS x;")]
    [TestCase("-- see /* below\r\nSELECT 1;")]
    [TestCase("-- arr[0\r\nSELECT 1;")]
    [TestCase("-- \"quoted\r\nSELECT 1;")]
    [TestCase("SELECT 1; -- not a stray */ either")]
    [TestCase("SELECT 1;--it's at the very end")]
    [TestCase("SELECT '--', 'it''s'; -- the first -- is in a string")]
    public void It_Ignores_Closures_Within_EOL_Comments(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }

    [Test]
    public void It_Still_Finds_Unclosed_Strings_After_A_Tick_In_An_EOL_Comment()
    {
        // The tick in "don't" used to PAIR with the unclosed string's tick - hiding the error.
        AssertSingleError(ClosureErrors("-- don't\r\nPRINT 'unclosed"), "Non-Terminated String.", 2, "'unclosed");
    }

    [Test]
    public void It_Does_Not_Treat_Dashes_In_A_String_As_A_Comment()
    {
        AssertSingleError(ClosureErrors("PRINT '-- not a comment"), "Non-Terminated String.", 1, "'-- not a comment");
    }

    // Openers in column 0 (any line) used to be reported as line -99.
    [TestCase("'abc", "Non-Terminated String.", 1)]
    [TestCase("SELECT 1;\r\n[Unclosed", "Non-Terminated [Identifier].", 2)]
    [TestCase("/* unclosed\r\nPRINT 'x';", "Non-Terminated Block-Comment.", 1)]
    [TestCase("SELECT 1;\r\n\r\n\"oops", "Non-Terminated \"Identifier\".", 3)]
    [TestCase("SELECT 1;\r\n*/", "Unexpected End of Block-Comment (*/ without a matching /*).", 2)]
    public void It_Reports_The_Correct_Line_For_Errors_In_Column_Zero(string text, string message, int lineNumber)
    {
        AssertSingleError(ClosureErrors(text), message, lineNumber);
    }

    // Line numbers must match the INPUT's lines - whatever its line endings are, and whatever they're normalized TO.
    [TestCase("\r\n", LineEndingsType.CrLf)]
    [TestCase("\r\n", LineEndingsType.Lf)]
    [TestCase("\r\n", LineEndingsType.Cr)]
    [TestCase("\n", LineEndingsType.CrLf)]
    [TestCase("\n", LineEndingsType.Lf)]
    [TestCase("\n", LineEndingsType.Cr)]
    [TestCase("\r", LineEndingsType.CrLf)]
    [TestCase("\r", LineEndingsType.Lf)]
    [TestCase("\r", LineEndingsType.Cr)]
    public void It_Reports_The_Correct_Line_Regardless_Of_Line_Endings(string lineEnding, LineEndingsType target)
    {
        var lines = Enumerable.Range(1, 30).Select(n => n == 21 ? "SELECT [oops" : $"SELECT {n};");
        string text = string.Join(lineEnding, lines);

        AssertSingleError(ClosureErrors(text, target), "Non-Terminated [Identifier].", 21, "[oops");
    }

    [Test]
    public void It_Reports_The_Correct_Line_With_Mixed_Line_Endings()
    {
        AssertSingleError(ClosureErrors("SELECT 1;\r\nSELECT 2;\nSELECT 3;\rSELECT 4;\r\n\nPRINT 'oops"), "Non-Terminated String.", 6, "'oops");
    }

    [Test]
    public void It_Reports_The_Line_Where_A_Multi_Line_Construct_Starts()
    {
        AssertSingleError(ClosureErrors("SELECT 1;\r\nPRINT N'line 2\r\nline 3\r\nline 4"), "Non-Terminated String.", 2, "'line 2");
    }

    // Nested block comments are valid T-SQL.
    [TestCase("/* outer /* inner */ still outer */\r\nSELECT 1;")]
    [TestCase("/*\r\n/* note */\r\nthis isn't code\r\n*/\r\nSELECT 1;")]
    [TestCase("/* a /* b /* c */ b */ a */ SELECT 1;")]
    [TestCase("/**/ SELECT 1; /***/ /* ** */")]
    public void It_Allows_Nested_Block_Comments(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }

    [TestCase("/* outer /* inner */\r\nSELECT 1;", 1)]
    [TestCase("SELECT 1;\r\n/* outer\r\n/* inner */\r\nSELECT 2;", 2)]
    public void It_Reports_Unclosed_Outer_Block_Comments(string text, int lineNumber)
    {
        AssertSingleError(ClosureErrors(text), "Non-Terminated Block-Comment.", lineNumber);
    }

    // "Quoted" identifiers (or strings - if QUOTED_IDENTIFIER is OFF): same rules either way.
    [TestCase("SELECT 1 AS \"Don't\";")]
    [TestCase("SELECT 1 AS \"a\"\"b\";")]
    [TestCase("SELECT 1 AS \"[x\", 2 AS \"/* y\", 3 AS \"-- z\";")]
    [TestCase("SELECT '\"' AS [\"], 2 AS \"'\";")]
    public void It_Ignores_Closures_Within_Quoted_Identifiers(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }

    [TestCase("SELECT 1 AS \"oops;", "\"oops;")]
    [TestCase("SELECT 1 AS \"a\"\";", "\"a\"\";")]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Quoted_Identifiers(string text, string detail)
    {
        AssertSingleError(ClosureErrors(text), "Non-Terminated \"Identifier\".", 1, detail);
    }

    [TestCase("SELECT 1 AS [a]]")]           // ]] is an escaped ] - so the identifier is still open.
    [TestCase("SELECT 1 AS [a]]]]")]
    public void It_Has_SyntaxErrors_When_Brackets_End_With_An_Escaped_Bracket(string text)
    {
        AssertSingleError(ClosureErrors(text), "Non-Terminated [Identifier].", 1);
    }

    [TestCase("SELECT 127 [kinda [weird]]];")]
    [TestCase("SELECT 1 AS [a]]]")]
    [TestCase("SELECT [O'Brien] FROM t;")]
    [TestCase("SELECT 'wth?' [for 'realz'?];")]
    [TestCase("SELECT 1 AS [this is --not a comment], 2 AS [/* nor this */];")]
    public void It_Ignores_Closures_Within_Bracketed_Identifiers(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }

    [TestCase("/* don't */ SELECT 1;")]
    [TestCase("SELECT '', N'', '''', N'''';")]
    [TestCase("SELECT 'it''s', 'a [b', 'c /* d', 'e \"f', 'g */ h';")]
    public void It_Handles_Correctly_Formed_Strings_And_Comments(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }

    [Test]
    public void It_Has_SyntaxErrors_When_A_String_Ends_With_An_Escaped_Tick()
    {
        AssertSingleError(ClosureErrors("PRINT 'abc'';"), "Non-Terminated String.", 1, "'abc'';");
    }

    // Like SQL Server: an unclosed string/comment/identifier swallows the REST of the file - so it's ONE error, not a cascade.
    [TestCase("/* unclosed\r\ndon't\r\nSELECT 1;", "Non-Terminated Block-Comment.", "/* unclosed")]
    [TestCase("[a\r\n[b", "Non-Terminated [Identifier].", "[a")]
    [TestCase("PRINT 'oops\r\nSELECT [x", "Non-Terminated String.", "'oops")]
    [TestCase("SELECT \"x\r\n/* y\r\n'z", "Non-Terminated \"Identifier\".", "\"x")]
    public void It_Reports_One_Error_For_One_Unclosed_Construct(string text, string message, string detail)
    {
        AssertSingleError(ClosureErrors(text), message, 1, detail);
    }

    [Test]
    public void It_Limits_Error_Detail_To_The_Rest_Of_The_Line()
    {
        // Detail used to be the ENTIRE rest of the file for strings (and to end with a stray \r for comments/brackets).
        AssertSingleError(ClosureErrors("PRINT 'oops\r\nSELECT 1;\r\nSELECT 2;\r\nSELECT 3;"), "Non-Terminated String.", 1, "'oops");
        AssertSingleError(ClosureErrors("SELECT 1; /* oops\r\nSELECT 2;"), "Non-Terminated Block-Comment.", 1, "/* oops");
        AssertSingleError(ClosureErrors("SELECT [oops\r\nSELECT 2;"), "Non-Terminated [Identifier].", 1, "[oops");
    }

    [TestCase("SELECT 1; */", 1, "*/")]
    [TestCase("SELECT 1;\r\n/* a */ */ -- the 2nd one", 2, "*/ -- the 2nd one")]
    public void It_Has_SyntaxErrors_For_Block_Comment_Ends_Without_A_Start(string text, int lineNumber, string detail)
    {
        AssertSingleError(ClosureErrors(text), "Unexpected End of Block-Comment (*/ without a matching /*).", lineNumber, detail);
    }

    [Test]
    public void It_Reports_Every_Block_Comment_End_Without_A_Start()
    {
        var errors = ClosureErrors("SELECT 1; */\r\nSELECT 2;\r\nSELECT 3; */");

        Assert.That(errors.Select(e => e.LineNumber), Is.EqualTo(new[] { 1, 3 }));
    }

    [TestCase("SELECT t.*/* all columns */ FROM t;")]       // * then a /* comment - NOT a stray */
    [TestCase("SELECT 2*/* times */3;")]
    [TestCase("PRINT '*/';")]
    [TestCase("SELECT 1 AS [*/], 2 AS \"*/\";")]
    [TestCase("/* a */ SELECT 1; /* b */")]
    public void It_Does_Not_Report_Valid_Uses_Of_Comment_Ends(string text)
    {
        AssertNoErrors(ClosureErrors(text));
    }
    #endregion

    #region Matches are ignored within strings
    [Test]
    public void It_Ignores_EOL_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an EOL comment -- but it should be ignored';\r\nSET @string = N'some value'; -- this is a legit EOL comment.";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
       
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(2));
    }

    [Test]
    public void It_Ignores_Block_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with a block comment /* but it should be ignored */';\r\n/* but this is a legit block \r\n comment */\r\nSET @string = N'some value';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(4));
    }

    [Test]
    public void It_Ignores_Escaped_Ticks_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an escaped tick '' and it should''t cause an error';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Ignores_GO_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with GO in it, but it should be ignored';\r\n\r\n/* this is a comment with GO in it - but it should be ignored */\r\nGO\r\nPRINT 'This is batch 2';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(5));
    }

    [Test]
    public void It_Ignores_BracketedIdentifiers_Within_Strings()
    {
        AssertNoErrors(ClosureErrors("SELECT 'this is not an identifier [so ignore me]' AS [columnName];"));
    }
    #endregion

    #region Matches are ignored within Comments
    [Test]
    public void It_Ignores_EOL_Comments_Within_Block_Comments()
    {
        var text = "/* this is a block comment with an EOL comment -- but it should be ignored */\r\nPRINT 'Hello World!';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var codeFile = new FileDetail("file-name.sql", 0, 0);

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, codeFile);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(2));
    }

    //[Test]
    //public void It_Ignores_GO_Within_Comments()
    //{
    //    var text = "/* this is a block comment with GO in it - but it should be ignored */\r\nGO\r\nPRINT 'This is batch 2';";
    //    var lines = new List<ICodeLine>();
    //    var errors = new List<ISyntaxError>();
    //    var stack = new Stack<IStackEntry>();
    //    stack.Push(new StackEntry("file-name.sql", 0, 0));

    //    var sut = new Normalizer();
    //    sut.Normalize(text, lines, errors, stack);
        
    //    Assert.That(errors.Count, Is.EqualTo(0));
    //    Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(2));  // i.e., there are 2 batches but NOT 3. 
    //    text = "DECLARE @oink int = 2;\r\n--GO";
    //    sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //    Assert.That(sut.EolComments.Count, Is.EqualTo(1));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}

    [Test]
    public void It_Ignores_BracketedIdentifiers_Within_Comments()
    {
        AssertNoErrors(ClosureErrors("/* this is a comment with [bracketed identifiers] in it - but ignore them */\r\nGO\r\nSELECT 1;"));
        AssertNoErrors(ClosureErrors("-- this is a comment with an [unclosed identifier in it\r\nGO\r\nSELECT 1;"));
    }
    #endregion

    #region Syntax Edge Cases 
    //[Test]
    //public void It_Ignores_Escaped_Brackets_Within_Identifiers()
    //{
    //    var text = "SELECT 127 [kinda [weird]]];";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //}
    //[Test]
    //public void It_Ignores_Go_Within_Bracketed_Identifiers()
    //{
    //    var text = "SELECT 'I''m not even mad, bro.' [Go go go];";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}
    //[Test]
    //public void It_Ignores_EOL_comments_Within_Bracketed_Identifiers()
    //{
    //    var text = "SELECT 'But, why?' AS [this is a --comment]";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //    Assert.That(sut.EolComments.Count, Is.EqualTo(0));
    //}
    //[Test]
    //public void It_Ignores_Block_Comments_Within_Bracketed_Identifiers()
    //{
    //    var text = "SELECT N'text' [this is /* nuts */]";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //    Assert.That(sut.BlockComments.Count, Is.EqualTo(0));
    //}
    //[Test]
    //public void It_Ignores_Strings_Within_Bracketed_Identifiers()
    //{
    //    var text = "SELECT 'wth?' [for 'realz'?]";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}
    //#endregion
    //#region Batch Splitting / Mapping
    //[Test]
    //public void It_Treats_Single_Block_of_Code_Without_Go_Statement_As_Batch()
    //{
    //    var text = "PRINT 'Hello World!';";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}
    //[Test]
    //public void It_Splits_On_Simple_Batches()
    //{
    //    var text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(2));
    //}
    //[Test]
    //public void It_Allows_WhiteSpace_After_Final_Go_Statement()
    //{
    //    var text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO ";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(3));
    //    StringAssert.AreEqualIgnoringCase(" ", sut.Batches[2].BatchText);
    //    text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO\r\n";
    //    sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(3));  // blank space IS _technically_ a batch.
    //    StringAssert.AreEqualIgnoringCase("\r\n", sut.Batches[2].BatchText);
    //}
    //[Test]
    //public void It_Does_Not_Confuse_Goto_With_Go()
    //{
    //    var text = "DECLARE @oink int = 2;\r\nIF @oink = 3 GOTO Piggy;\r\nELSE GOTO EndPiggy;\r\n\r\nPiggy:\r\nPRINT 'Oink!';\r\n\r\nEndPiggy:\r\nGO";
    //    var sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}
    //[Test]
    //public void It_Requires_WhiteSpace_Between_Go_And_Count()
    //{
    //    var text = "DBCC CHECKPOINT;\r\nGO3\r\n";
    //    var sut = new Mapper(text);
    //    // BECAUSE I've got trailing space after the 'go', this WOULD be 2 batches IF GO3 was treated as a batch terminator. 
    //    //  it should NOT be - it's not formed correctly (i.e., should be "GO 3" not "GO3"). 
    //    Assert.That(sut.Batches.Count, Is.EqualTo(1));
    //}
    //[Test]
    //public void It_Supports_Go_With_Count()
    //{
    //    var text = "DBCC CHECKPOINT;\r\nGO 3\r\n"; // correctly formatted. 
    //    var sut = new Mapper(text);
    //    Assert.That(sut.Batches.Count, Is.EqualTo(2));  // whitespace after GO is, techincally, a batch.
    //    StringAssert.AreEqualIgnoringCase("GO 3", sut.Batches[0].GoStatement);
    //    //Assert.That(sut.Batches[1].GoCount, Is.EqualTo(3));
    //}
    #endregion

    #region EOL Comment Mapping and Processing
    //[Test]
    //public void It_Preserves_EolComments_And_WhiteSpace_When_Transforming_GoOnlyBatches()
    //{
    //    var text = "\r\n/* this is terrible - but valid */  USE admindb;  -- there's whitespace before the GO + a tick in this comment... \r\nGO";
    //    // i.e., need to run a transform on the above and ... will expect that GO is gone ... but that there's a blank line whee it was.
    //    //  and that comments are still in place. 
    //    Assert.Fail("Not implemented");
    //}
    #endregion

    #region Block Comment Mapping
    //[Test]
    //public void It_Allows_Block_Comments_Before_Go_Without_SemiColon()
    //{
    //    var text = "\r\n/* this is terrible - but valid */  USE admindb  -- no semi-colon after the USE ...  \r\nGO";
    //}
    //[Test]
    //public void It_Allows_Block_Comments_Before_Go_With_SemiColon()
    //{
    //    var text = "\r\n/* this is terrible - but valid */  USE admindb;  -- no semi-colon after the USE ...  \r\nGO";
    //}
    #endregion

    #region DDL Mapping
    // TODO: it captures CREATE PROC statements
    // TODO: it captures CREATE FUNCTION statements
    // TODO: it captures CREATE VIEW statements
    // TODO: it captures CREATE TRIGGER statements
    // TODO: it captures CREATE TYPE statements
    // TODO: it captures CREATE AGGREGATE statements
    // TODO: it captures CREATE ASSEMBLY statements
    // TODO: it captures CREATE TABLE statements
    // etc... 
    // TODO: it captures ALTER statements
    // TODO: it captures CREATE OR ALTER statements
    #endregion
}