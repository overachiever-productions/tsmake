namespace tsmake.tests.unit_tests;

public class StackTests
{
    [Test]
    public void Stacks_Correctly_Report_StackDepth()
    {
        // Sanity-Check that Stack<T> is working ... and that Depth ...matches. 
        var stack = new Stack<IStackEntry>();
        Assert.That(stack.Count, Is.EqualTo(0));

        stack.Push(new StackEntry("test_build.sql", 0, stack.Count));
        Assert.That(stack.Count, Is.EqualTo(1));
        var entry = stack.Peek();
        Assert.That(entry.Depth, Is.EqualTo(0));

        stack.Push(new StackEntry("test_file1.sql", 18, stack.Count));
        Assert.That(stack.Count, Is.EqualTo(2));
        entry = stack.Peek();
        Assert.That(entry.Depth, Is.EqualTo(1));

        stack.Pop();
        Assert.That(stack.Count, Is.EqualTo(1));
        entry = stack.Peek();
        Assert.That(entry.Depth, Is.EqualTo(0));

        stack.Pop();
        Assert.That(stack.Count, Is.EqualTo(0));
    }
}