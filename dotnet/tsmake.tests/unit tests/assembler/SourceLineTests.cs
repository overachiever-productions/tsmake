namespace tsmake.tests.unit_tests.assembler;

public class SourceLineTests
{
    [Test]
    public void SourceLine_Shows_Depth_Zero_On_No_Ancestors()
    {
        var stack = new Stack<string>();
        var sut = new SourceLine(3, "Fake Line Text for Line # 3", new Stack<string>(stack));

        Assert.That(sut.Depth, Is.EqualTo(0));
    }

    [Test]
    public void SourceLine_Shows_Depth_Of_One_On_Single_Ancestor()
    {
        var stack = new Stack<string>();
        stack.Push(@"D:\FakeDir\ParentFile.build.sql");

        var sut = new SourceLine(3, "Fake Line Text for Line # 3", new Stack<string>(stack));

        Assert.That(sut.Depth, Is.EqualTo(1));
    }

    [Test]
    public void SourceLine_Shows_Current_File_Name_As_FileName_Property()
    {
        var childFile = @"D:\FakeDir\SomeFile.sql";
        var parentFile = @"D:\FakeDir\SomeFile.sql";

        var stack = new Stack<string>();
        stack.Push(parentFile);
        
        var sut = new SourceLine(3, "Fake Line Text for Line # 3", new Stack<string>(stack));

        Assert.That(sut.Depth, Is.EqualTo(1));
        StringAssert.AreEqualIgnoringCase(childFile, sut.FileName);
    }

    [Test]
    public void SourceLine_PrintStack_Does_Not_Pop_Stack()
    {
        var stack = new Stack<string>();
        stack.Push(@"D:\FakeDir\ParentFile.build.sql");

        // Simulate Assembler .Push()ing a new file. (Note, SourceLines do NOT .Push()) current file, assembler does. 
        stack.Push(@"D:\FakeDir\SomeFile.sql");

        var sut = new SourceLine(3, "Fake Line Text for Line # 3", new Stack<string>(stack));

        Assert.That(sut.Depth, Is.EqualTo(2));

        string printedStack = sut.Stack.PrintStack();
        var lines = printedStack.Split('\n');

        Assert.That(sut.Depth, Is.EqualTo(2));
        Assert.That(lines.Length, Is.EqualTo(2));
    }

    [Test]
    public void SourceLine_PrintStack_Correctly_Formats_Stack_Data()
    {
        var stack = new Stack<string>();
        stack.Push(@"D:\FakeDir\ParentFile.build.sql");

        // Simulate Assembler .Push()ing a new file.
        // Note: SourceLines do NOT .Push() current file, Assembler does. 
        stack.Push(@"D:\FakeDir\SomeFile.sql");

        var sut = new SourceLine(3, "Fake Line Text for Line # 3", new Stack<string>(stack));
        string printedStack = sut.Stack.PrintStack();

        var expectedOutput = "D:\\FakeDir\\SomeFile.sql\r\n\t  -> D:\\FakeDir\\ParentFile.build.sql";

        StringAssert.AreEqualIgnoringCase(expectedOutput, printedStack);
        Assert.That(sut.Depth, Is.EqualTo(2));
    }
}