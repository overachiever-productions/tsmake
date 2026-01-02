namespace tsmake.tests.unit_tests.mapper;

public class MapperTests
{
    // it 'has' syntaxerrors when there are unclosed 'strings
    // it 'has' syntaxerrors when there are unclosed /* block comments 
    // it 'has' syntaxerrors when there are unclosed [brackets

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
        Assert.That(sut.Batches.Count, Is.EqualTo(2));

        text = "PRINT N'Hello World!';\r\nGO\r\nPRINT N'Batch 2';\r\nGO\r\n";

        sut = new Mapper(text);
        Assert.That(sut.Batches.Count, Is.EqualTo(2));
    }

    [Test]
    public void It_Treats_Single_Block_of_Code_Without_Go_Statement_As_Batch()
    {
        Assert.Fail("There's actually no logic for this yet within mapper"); 
    }

    [Test]
    public void It_Does_Not_Confuse_Goto_With_Go()
    {
        Assert.Fail("Not implemented");
    }

    [Test]
    public void It_Requires_WhiteSpace_Between_Go_And_Count()
    {
        // this is invalid - and should throw: 
        var text = "PRINT 'huh';\r\nGO3";
        //var sut = new BatchSplitter(text, new BatchSplitOptions(false, false, false));

        //Assert.Throws<TokenizerException>(() => sut.SplitBatches());

        // MKC: I don't think this should throw... i just should NOT match is all. 

        Assert.Fail("Not implemented");
    }

    // TODO: think I want to convert these to 'convert' tests: 
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

    [Test]
    public void It_Preserves_EolComments_And_WhiteSpace_When_Transforming_GoOnlyBatches()
    {
        var text = "\r\n/* this is terrible - but valid */  USE admindb;  -- there's whitespace before the GO + a tick in this comment... \r\nGO";

        // i.e., need to run a transform on the above and ... will expect that GO is gone ... but that there's a blank line whee it was.
        //  and that comments are still in place. 
        Assert.Fail("Not implemented");
    }

    // it_can_handle_escaped_ticks_in_strings

    // it does not get GO inside strings or comments

    // it does not collect DDL within comments or strings. 
}
