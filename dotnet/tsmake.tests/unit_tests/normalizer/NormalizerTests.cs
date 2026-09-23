namespace tsmake.tests.unit_tests.normalizer;

// TODO: 
//     need to add a whole SUITE of tests that verify that CodeLines passed IN to a normalizer are preserved and not modified.
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
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Does_Not_Split_Empty_Strings()
    {
        var text = string.Empty;
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(string.Empty, sut.NormalizedText);
    }

    [Test]
    public void It_Does_Not_Add_LineEndings_On_Single_Line_Inputs()
    {
        var text = "123456789ABCDEF";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_CrLf()
    {
        var text = "first-line\r\nsecond-line\r\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Lf()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Lf_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\nsecond-line\nthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Cr()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
    }

    [Test]
    public void It_Normalizes_Cr_Splits_To_Target_LineSplitOptions()
    {
        var text = "first-line\rsecond-line\rthird-line";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("first-line\r\nsecond-line\r\nthird-line", sut.NormalizedText);
    }

    [Test]
    public void It_Normalizes_To_Specified_LineEndingOptions()
    {
        var text = "first\r\nsecond\r\nthird\r\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer(LineEndingOptions.Cr);
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\rsecond\rthird\rfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Splits_On_Mixed_Line_Endings()
    {
        var text = "first\r\nsecond\rthird\nfourth";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(4));
        StringAssert.AreEqualIgnoringCase("first\r\nsecond\r\nthird\r\nfourth", sut.NormalizedText);
    }

    [Test]
    public void It_Keeps_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\r\nline2\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(text, sut.NormalizedText);
    }

    [Test]
    public void It_Normalized_Empty_Lines_At_End_Of_Input()
    {
        var text = "line1\rline2\r";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase("line1\r\nline2\r\n", sut.NormalizedText);
    }

    [Test]
    public void It_Calibrates_Line_Endings_Correctly()
    {
        var text = "1\r\n22\r\n333\r\n4444\r\n\r\n666666\r\n7777777\r\n";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

        Assert.That(lines.Count, Is.EqualTo(8));

        StringAssert.AreEqualIgnoringCase("1", lines[0].Content);
        StringAssert.AreEqualIgnoringCase("22", lines[1].Content);
        StringAssert.AreEqualIgnoringCase("333", lines[2].Content);
        StringAssert.AreEqualIgnoringCase("4444", lines[3].Content);
        StringAssert.AreEqualIgnoringCase("", lines[4].Content);
        StringAssert.AreEqualIgnoringCase("666666", lines[5].Content);
        StringAssert.AreEqualIgnoringCase("7777777", lines[6].Content);
        StringAssert.AreEqualIgnoringCase("", lines[7].Content);

        Assert.That(lines[0].LineNumber, Is.EqualTo(1));
        Assert.That(lines[1].LineNumber, Is.EqualTo(2));
        Assert.That(lines[2].LineNumber, Is.EqualTo(3));
        Assert.That(lines[3].LineNumber, Is.EqualTo(4));
        Assert.That(lines[4].LineNumber, Is.EqualTo(5));
        Assert.That(lines[5].LineNumber, Is.EqualTo(6));
        Assert.That(lines[6].LineNumber, Is.EqualTo(7));
        Assert.That(lines[7].LineNumber, Is.EqualTo(8));

        Assert.That(lines[0].Content.Length, Is.EqualTo(1));
        Assert.That(lines[1].Content.Length, Is.EqualTo(2));
        Assert.That(lines[2].Content.Length, Is.EqualTo(3));
        Assert.That(lines[3].Content.Length, Is.EqualTo(4));
        Assert.That(lines[4].Content.Length, Is.EqualTo(0));
        Assert.That(lines[5].Content.Length, Is.EqualTo(6));
        Assert.That(lines[6].Content.Length, Is.EqualTo(7));
        Assert.That(lines[7].Content.Length, Is.EqualTo(0));
    }
    #endregion

    #region Closure Validations
    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Strings()
    {
        var text = "PRINT 'Hello World!; -- note the missing end-tick... ";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);

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
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Correctly_Identifies_Location_Of_Unclosed_Strings()
    {
        var text = "DECLARE @simple sysname = N'this is simple';\r\nDECLARE @complex sysname = N'this is complex with a comment /* and a string '' and an unclosed string '' and an unclosed comment /*';\r\nDECLARE @multiline sysname = N'this spans\r\nmultiple\r\nlines';\r\nDECLARE @butThisisBad sysname = N'total fail\r\n-- closing comment. ";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
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
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated Block-Comment", errors[0].Message);
    }

    [Test]
    public void It_Ignores_Unclosed_BlockComments_Within_Strings()
    {
        var text = "DECLARE @anotherString nvarchar(max) = N'this is not an unclosed block comment /* ';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_BracketIdentifiers()
    {
        var text = "SELECT * FROM [MyTable;\r\nGO";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated", errors[0].Message);
    }

    [Test]
    public void It_Ignores_Unclosed_BracketIdentifiers_Within_Strings()
    {
        var text = "DECLARE @mytext nvarchar(MAX) = N'this is not [a real identifier';\r\nGO";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
    }
    #endregion

    #region Matches are ignored within strings
    [Test]
    public void It_Ignores_EOL_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an EOL comment -- but it should be ignored';\r\nSET @string = N'some value'; -- this is a legit EOL comment.";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
       
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(2));
    }

    [Test]
    public void It_Ignores_Block_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with a block comment /* but it should be ignored */';\r\n/* but this is a legit block \r\n comment */\r\nSET @string = N'some value';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(4));
    }

    [Test]
    public void It_Ignores_Escaped_Ticks_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an escaped tick '' and it should''t cause an error';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Ignores_GO_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with GO in it, but it should be ignored';\r\n\r\n/* this is a comment with GO in it - but it should be ignored */\r\nGO\r\nPRINT 'This is batch 2';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
        Assert.That(errors.Count, Is.EqualTo(0));
        Assert.That(lines.Count, Is.EqualTo(5));
    }

    //  TODO:
    // It_Ignores_BracketedIdentifiers_Within_Strings() - e.g.,  SELECT 'this is not an identifier [so ignore me]' AS [columnName];
    #endregion

    #region Matches are ignored within Comments
    [Test]
    public void It_Ignores_EOL_Comments_Within_Block_Comments()
    {
        var text = "/* this is a block comment with an EOL comment -- but it should be ignored */\r\nPRINT 'Hello World!';";
        var lines = new List<ICodeLine>();
        var errors = new List<ISyntaxError>();
        var stack = new Stack<IStackEntry>();
        stack.Push(new StackEntry("file-name.sql", 0, 0));

        var sut = new Normalizer();
        sut.Normalize(text, lines, errors, stack);
        
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

    // TODO:
    // It_Ignores_BracketedIdentifiers_Within_Comments() - e.g.,  /* this is a comment with [bracketed identifiers] in it - but ignore them */\r\nGO\r\nSELECT 1;
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