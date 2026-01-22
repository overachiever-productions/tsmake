namespace tsmake.tests.unit_tests.mapper;

public class MapperTests
{
    #region Closure Validations
    // it 'has' syntaxerrors when there are unclosed 'strings
    // it 'has' syntaxerrors when there are unclosed /* block comments 
    // it 'has' syntaxerrors when there are unclosed [brackets
    #endregion

    #region Token Matches in Strings are Ignored
    // it_can_handle_escaped_ticks_in_strings

    // it does not get GO inside strings or comments

    // it does not collect DDL within comments or strings. 
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
}
