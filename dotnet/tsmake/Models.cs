namespace tsmake;

public class Batch : IBatch
{
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string BatchText { get; }
    public string GoStatement { get; }
    public int GoCount { get; }
    public bool IsUseOnlyBatch { get; }
    public bool IsGoOnlyBatch { get; }

    public Batch(string batchText, string goStatement, int start, int end)
    {
        this.BatchText = batchText;
        this.GoStatement = goStatement;
        this.StartOffset = start;
        this.EndOffset = end;
    }
}

public class ObjectDeclaration : IObjectDeclaration
{
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string Text { get; }
    public string ObjectType { get; }
    public string ObjectName { get; }

    public ObjectDeclaration(string ddlStart, int start, int end)
    {
        this.Text = ddlStart;
        this.StartOffset = start;
        this.EndOffset = end;

        // might need to have some sort of ObjectDeclarationStart type of object or some way to start with some simple stuff and then refine it later.
        //  and... hell. I could LEGIT pass in the next few hundred characters after the ddlStart to help with parsing out the object type/name.
        this.ObjectType = "UNKNOWN";
        this.ObjectName = "UNKNOWN";
    }
}

public class EolComment : IEolComment
{
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string CommentText { get; }

    public EolComment(string commentText, int start, int end)
    {
        this.CommentText = commentText;
        this.StartOffset = start;
        this.EndOffset = end;
    }
}

public class BlockComment : IBlockComment
{
    public int StartOffset { get; }
    public int EndOffset { get; }
    public string CommentText { get; }

    public BlockComment(string commentText, int start, int end)
    {
        this.CommentText = commentText;
        this.StartOffset = start;
        this.EndOffset = end;
    }
}