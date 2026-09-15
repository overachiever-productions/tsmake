namespace tsmake.tests.unit_tests.mapper;

public class MapperTests
{
    #region Conventions
    // TODO: it requires INormalizedString for input... (MKC: actually, not sure this needs to be a test. if I change the .ctor ... then ... there's no test in question).
    #endregion

    #region Closure Validations
    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Strings()
    {
        var text = "PRINT 'Hello World!; -- note the misssing end-tick... ";
        var sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated String.", sut.SyntaxErrors[0].Message);
    }

    [Test]
    public void It_Does_Not_Have_Syntax_Errors_For_Correctly_Formed_Strings()
    {
        // NOTE: All of the following are correctly formed: 
        var text = "DECLARE @simple sysname = N'this is simple';\r\nDECLARE @complex sysname = N'this is complex with a comment /* and a string '' and an unclosed string '' and an unclosed comment /*';\r\nDECLARE @multiline sysname = N'this spans\r\nmultiple\r\nlines';";
        var sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Correctly_Identifies_Location_Of_Unclosed_Strings()
    {
        var text = "DECLARE @simple sysname = N'this is simple';\r\nDECLARE @complex sysname = N'this is complex with a comment /* and a string '' and an unclosed string '' and an unclosed comment /*';\r\nDECLARE @multiline sysname = N'this spans\r\nmultiple\r\nlines';\r\nDECLARE @butThisisBad sysname = N'total fail\r\n-- closing comment. ";
        var sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated String.", sut.SyntaxErrors[0].Message);

        Assert.That(sut.SyntaxErrors[0].StartOffset, Is.EqualTo(276));
    }

    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_Block_Comments()
    {
        var text = "/* This is an unclosed block comment\r\nPRINT 'Hello World!';";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated Block-Comment", sut.SyntaxErrors[0].Message);
    }

    [Test]
    public void It_Ignores_Unclosed_BlockComments_Within_Strings()
    {
        var text = "DECLARE @anotherString nvarchar(max) = N'this is not an unclosed block comment /* ';";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Has_SyntaxErrors_When_There_Are_Unclosed_BracketIdentifiers()
    {
        var text = "SELECT * FROM [MyTable;\r\nGO";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(1));
        StringAssert.Contains("Non-Terminated", sut.SyntaxErrors[0].Message);
    }

    [Test]
    public void It_Ignores_Unclosed_BracketIdentifiers_Within_Strings()
    {
        var text = "DECLARE @mytext nvarchar(MAX) = N'this is not [a real identifier';\r\nGO";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    }
    #endregion

    #region Matches are ignored within strings
    [Test]
    public void It_Ignores_EOL_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an EOL comment -- but it should be ignored';\r\nSET @string = N'some value'; -- this is a legit EOL comment.";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.EolComments.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Ignores_Block_Comments_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with a block comment /* but it should be ignored */';\r\n/* but this is a legit block \r\n comment */\r\nSET @string = N'some value';";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Ignores_Escaped_Ticks_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with an escaped tick '' and it should''t cause an error';";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Ignores_GO_Within_Strings()
    {
        var text = "DECLARE @string nvarchar(MAX) = N'this is a string with GO in it, but it should be ignored';\r\n\r\n/* this is a comment with GO in it - but it should be ignored */\r\nGO\r\nPRINT 'This is batch 2';";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.Batches.Count, Is.EqualTo(2));

        StringAssert.AreEqualIgnoringCase("DECLARE @string nvarchar(MAX) = N'this is a string with GO in it, but it should be ignored';\r\n\r\n/* this is a comment with GO in it - but it should be ignored */\r\nGO", sut.Batches[0].BatchText);
        StringAssert.AreEqualIgnoringCase("GO", sut.Batches[0].GoStatement);
        StringAssert.AreEqualIgnoringCase("", sut.Batches[1].GoStatement);
    }
    //  TODO:
    // It_Ignores_BracketedIdentifiers_Within_Strings() - e.g.,  SELECT 'this is not an identifier [so ignore me]' AS [columnName];
    #endregion

    #region Matches are ignored within Comments
    [Test]
    public void It_Ignores_EOL_Comments_Within_Block_Comments()
    {
        var text = "/* this is a block comment with an EOL comment -- but it should be ignored */\r\nPRINT 'Hello World!';";
        var sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));

        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.EolComments.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Ignores_GO_Within_Comments()
    {
        var text = "/* this is a block comment with GO in it - but it should be ignored */\r\nGO\r\nPRINT 'This is batch 2';";
        var sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(1));
        Assert.That(sut.Batches.Count, Is.EqualTo(2));  // i.e., there are 2 batches but NOT 3. 

        text = "DECLARE @oink int = 2;\r\n--GO";
        sut = new Mapper(text);
        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.EolComments.Count, Is.EqualTo(1));
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }
    // TODO:
    // It_Ignores_BracketedIdentifiers_Within_Comments() - e.g.,  /* this is a comment with [bracketed identifiers] in it - but ignore them */\r\nGO\r\nSELECT 1;
    #endregion

    #region Syntax Edge Cases 
    [Test]
    public void It_Ignores_Escaped_Brackets_Within_Identifiers()
    {
        var text = "SELECT 127 [kinda [weird]]];";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Ignores_Go_Within_Bracketed_Identifiers()
    {
        var text = "SELECT 'I''m not even mad, bro.' [Go go go];";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Ignores_EOL_comments_Within_Bracketed_Identifiers()
    {
        var text = "SELECT 'But, why?' AS [this is a --comment]";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
        Assert.That(sut.EolComments.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Ignores_Block_Comments_Within_Bracketed_Identifiers()
    {
        var text = "SELECT N'text' [this is /* nuts */]";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
        Assert.That(sut.BlockComments.Count, Is.EqualTo(0));
    }

    [Test]
    public void It_Ignores_Strings_Within_Bracketed_Identifiers()
    {
        var text = "SELECT 'wth?' [for 'realz'?]";
        var sut = new Mapper(text);

        Assert.That(sut.SyntaxErrors.Count, Is.EqualTo(0));
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }
    #endregion

    #region Batch Splitting / Mapping
    [Test]
    public void It_Treats_Single_Block_of_Code_Without_Go_Statement_As_Batch()
    {
        var text = "PRINT 'Hello World!';";
        var sut = new Mapper(text);

        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Splits_On_Simple_Batches()
    {
        var text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO";
        var sut = new Mapper(text);

        Assert.That(sut.Batches.Count, Is.EqualTo(2));
    }

    [Test]
    public void It_Allows_WhiteSpace_After_Final_Go_Statement()
    {
        var text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO ";
        var sut = new Mapper(text);
        Assert.That(sut.Batches.Count, Is.EqualTo(3));
        StringAssert.AreEqualIgnoringCase(" ", sut.Batches[2].BatchText);

        text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO\r\n";

        sut = new Mapper(text);
        Assert.That(sut.Batches.Count, Is.EqualTo(3));  // blank space IS _technically_ a batch.
        StringAssert.AreEqualIgnoringCase("\r\n", sut.Batches[2].BatchText);
    }

    [Test]
    public void It_Does_Not_Confuse_Goto_With_Go()
    {
        var text = "DECLARE @oink int = 2;\r\nIF @oink = 3 GOTO Piggy;\r\nELSE GOTO EndPiggy;\r\n\r\nPiggy:\r\nPRINT 'Oink!';\r\n\r\nEndPiggy:\r\nGO";
        var sut = new Mapper(text);

        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Requires_WhiteSpace_Between_Go_And_Count()
    {
        var text = "DBCC CHECKPOINT;\r\nGO3\r\n";
        var sut = new Mapper(text);

        // BECAUSE I've got trailing space after the 'go', this WOULD be 2 batches IF GO3 was treated as a batch terminator. 
        //  it should NOT be - it's not formed correctly (i.e., should be "GO 3" not "GO3"). 
        Assert.That(sut.Batches.Count, Is.EqualTo(1));
    }

    [Test]
    public void It_Supports_Go_With_Count()
    {
        var text = "DBCC CHECKPOINT;\r\nGO 3\r\n"; // correctly formatted. 
        var sut = new Mapper(text);

        Assert.That(sut.Batches.Count, Is.EqualTo(2));  // whitespace after GO is, techincally, a batch.
        StringAssert.AreEqualIgnoringCase("GO 3", sut.Batches[0].GoStatement);


        //Assert.That(sut.Batches[1].GoCount, Is.EqualTo(3));
    }
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