namespace tsmake;

public interface ILocation 
{
    // file-stack. 
    // lineNumber  
}


// REFACTOR: Might collapse ISyntaxError and IRuntimeError into a single IError interface.
public interface ISyntaxError 
{
    int StartOffset { get; }
    int EndOffset { get; }
    string Message { get; }
    void SetLocation(ILocation location);
    string Summarize();     // used for xml formatter/CLI output.
}

public interface IRuntimeError
{
    ErrorRecord ErrorRecord { get; }
    string Message { get; }
    string Summarize();     // used for xml formatter/CLI output.
}

public interface INormalizer
{
    string NormalizedText { get; }
    List<CodeLine> Lines { get; }
}

public interface IBatch
{
    int StartOffset { get; }
    int EndOffset { get; }
    string BatchText { get; }
    string GoStatement { get; }
    int GoCount { get; }
    bool IsUseOnlyBatch { get; }
    bool IsGoOnlyBatch { get; }
}

public interface IEolComment
{
    int StartOffset { get; }
    int EndOffset { get; }
    string CommentText { get; }
}

public interface IBlockComment
{
    int StartOffset { get; }
    int EndOffset { get; }
    string CommentText { get; }
}

public interface IObjectDeclaration
{
    int StartOffset { get; }
    string ObjectType { get; }
    string ObjectName { get; }
}

public interface IMapper
{
    List<ISyntaxError> SyntaxErrors { get; }
    List<IBatch> Batches { get; }
    List<IEolComment> EolComments { get; }
    List<IBlockComment> BlockComments { get; }
    List<IObjectDeclaration> ObjectDeclarations { get; }
}

//public interface IManagedFile
//{

//}