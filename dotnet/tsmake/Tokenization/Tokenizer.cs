namespace tsmake;

public enum TokenType
{
    BlockComment,
    EolComment,
    String,
    GoStatement //, 
    //Identifier,
    //QuotedIdentifier, 
    //TempObjectIdentifier,  // # or ## 
    //Parameter, 
    //SystemVariable, // i.e., @@xx
    //Operator, 
    //Literal (number)
    //etc...
}

[Flags]
public enum NewLineStatus
{
    None = 0,
    CrFoundWaitingOnLf = 1
}

[Flags]
public enum StringStatus
{
    None = 0,
    InString = 1,
    EscapingOrNesting = 2
}

[Flags]
public enum CommentStatus
{
    None = 0,
    InComment = 1
}

[Flags]
public enum BlockCommentStatus
{
    None = 0,
    SlashFoundWaitingOnStar = 1,
    InComment = 2,
    Nested = 4,
    NestedStarFoundWaitingOnNestedBackslash = 8
}

public class CharacterBuffer(int limit)
{
    private readonly List<char> _buffer = new()
    {
        Capacity = limit
    };

    public int Limit { get; private set; }

    public void Add(char character)
    {
        this._buffer.Add(character);
        this._buffer.TrimExcess();
    }

    public char Peek()
    {
        return this._buffer[^1];
    }
}

public interface ITokenInitializer
{
    bool Handles(char character);
    void Process(ITokenizer tokenizer, StringReader reader, char currentChar);
}

public interface ITokenFinalizer
{
    bool WatchesFor(char character);
    void Process(ITokenizer tokenizer, StringReader reader, char currentChar);
    void ProcessRemoval(ITokenizer tokenizer);
    void Terminate(ITokenizer tokenizer);
}

public interface ICodeLine
{
    int LineNumber { get; }
    string Text { get; }
    int OffsetStart { get; }  // This is the SAME as the .LineOffsetStart
    int OffsetEnd { get; }

    void SetLineNumber(int lineNumber);
}

public class CodeLine(string lineText, int startOffset, int endOffset) : ICodeLine
{
    public int LineNumber { get; private set; }
    public string Text { get; private set; } = lineText;
    public int OffsetStart { get; private set; } = startOffset;
    public int OffsetEnd { get; private set; } = endOffset;

    public void SetLineNumber(int lineNumber)
    {
        this.LineNumber = lineNumber;
    }
}

public interface IToken<T>
{
    // REFACTOR: I've got a handful of copy/paste implementations that aren't that variable - i.e., use a 'TokenBase' for core functionality 
    TokenType TokenType { get; }            // REFACTOR: for my purposes, this is pretty redundant. i could easily nuke it and probably be just fine. 
    int OffsetStart { get;  }
    int OffsetEnd { get; }
    string Text { get; }

    int StartLine { get; }  
    int StartLineOffset { get; } 
    int ColumnStart { get; }

    T Clone();
}

public class CodeString(int start, int end, int lineStart, int lineStartOffset, string text, bool isUnicode) : IToken<CodeString>
{
    public TokenType TokenType { get; } = TokenType.String;
    public int OffsetStart { get; } = start;
    public int OffsetEnd { get; } = end;
    public string Text { get; } = text;
    public int StartLine { get; } = lineStart;
    public int StartLineOffset { get; } = lineStartOffset;
    public int ColumnStart => 1 + this.OffsetStart - this.StartLineOffset;
    public bool IsUnicode { get; } = isUnicode;

    public CodeString Clone()
    {
        return new CodeString(this.OffsetStart, this.OffsetEnd, this.StartLine, this.StartLineOffset, this.Text, this.IsUnicode);
    }
}

public class GoStatement(int startIndex, int endIndex, int lineStart, int lineStartOffset, string text, int goCount) : IToken<GoStatement>
{
    public TokenType TokenType { get; } = TokenType.GoStatement;
    public int OffsetStart { get; } = startIndex;
    public int OffsetEnd { get; } = endIndex;
    public string Text { get; } = text;
    public int StartLine { get; } = lineStart;
    public int StartLineOffset { get; } = lineStartOffset;
    public int ColumnStart => 1 + this.OffsetStart - this.StartLineOffset;
    public int GoCount { get; } = goCount;

    public GoStatement Clone()
    {
        return new GoStatement(this.OffsetStart, this.OffsetEnd, this.StartLine, this.StartLineOffset, this.Text, this.GoCount);
    }
}

public class BlockComment(int startIndex, int endIndex, int lineStart, int lineStartOffset, string text) : IToken<BlockComment>
{
    public TokenType TokenType { get; } = TokenType.BlockComment;
    public int OffsetStart { get; } = startIndex;
    public int OffsetEnd { get; } = endIndex;
    public string Text { get; } = text;
    public int StartLine { get; } = lineStart;
    public int StartLineOffset { get; } = lineStartOffset;
    public int ColumnStart => 1 + this.OffsetStart - this.StartLineOffset;

    public BlockComment Clone()
    {
        return new BlockComment(this.OffsetStart, this.OffsetEnd, this.StartLine, this.StartLineOffset, this.Text);
    }
}

public class Comment(int startIndex, int endIndex, int lineStart, int lineStartOffset, string text = "") : IToken<Comment>
{
    public TokenType TokenType { get; } = TokenType.EolComment;
    public int OffsetStart { get; } = startIndex;
    public int OffsetEnd { get; } = endIndex;
    public string Text { get; } = text;
    public int StartLine { get; } = lineStart;
    public int StartLineOffset { get; } = lineStartOffset;
    public int ColumnStart => 1 + this.OffsetStart - this.StartLineOffset;

    public Comment Clone()
    {
        return new Comment(this.OffsetStart, this.OffsetEnd, this.StartLine, this.StartLineOffset, this.Text);
    }
}

// MIGHT need to rename this. This is a USE [xxxx]; ... directive. i.e., a USE...Directive.
public class UseDirective(string text)
{
    // should I also pass in locations? ... if so, they need to be relative to the ... batch not the original document, right? 
    // and... i guess if I pass in "uSe xyz ... -- comments or whatever ... " then I can parse that crap out of the text parameter and ... 
    //  turn that into a db name via regex... 

    // which means that I should have a: 
    public string TargetDatabase { get; } = text; // TODO: just assigning this ... here to avoid breaking the build from within PowerShell. 
}

public class TextSources(string originalCommand, string originalBatch)
{
    public string OriginalCommand { get; } = originalCommand;
    public string OriginalBatch { get; } = originalBatch;
}

public class ParsedBatch(int start, int end, string text, TextSources sources)
{
    public int StartIndex { get; } = start;
    public int EndIndex { get; } = end;
    public string BatchText { get; } = text;
    public TextSources TextSources { get; set; } = sources;

    public GoStatement GoStatement { get; internal set; }

    public List<Comment> Comments { get; internal set; }
    public List<BlockComment> BlockComments { get; internal set; }
    public List<CodeString> Strings { get; internal set; }

    public List<UseDirective> UseDirectives
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}

public interface ITokenizer
{
    string RawText { get; }
    int CurrentIndex { get; }
    NewLineStatus NewLineStatus { get; set; }
    StringStatus StringStatus { get; set; }
    BlockCommentStatus BlockCommentStatus { get; set; }
    CommentStatus CommentStatus { get; set; }

    void EnlistInitializer(ITokenInitializer initializer);
    void EnlistFinalizer(ITokenFinalizer finalizer);
    void MarkFinalizerForRemoval(ITokenFinalizer finalizer);

    CharacterBuffer CharacterBuffer { get; }
    int CurrentLineNumber { get; }

    List<ICodeLine> CodeLines { get; }
    List<CodeString> Strings { get; }
    List<GoStatement> GoStatements { get; } // and/or should i have .Batches?
    List<BlockComment> BlockComments { get; }
    List<Comment> Comments { get; }
    int BlockCommentNestingLevel { get; set; }

    void Tokenize();

    List<ParsedBatch> GetParsedBatches(bool ignoreGoInUseOnlyBatches);

    void AddCodeLineFromCurrentLocation();

    int GetCurrentLineStartOffset();
    int GetLineStartOffsetByOffset(int offset);
    CodeLine GetCurrentLineFromCurrentLocation();
}

public class Tokenizer : ITokenizer
{
    private List<ITokenInitializer> _tokenInitializers = new List<ITokenInitializer>();
    private List<ITokenFinalizer> _tokenFinalizers = new List<ITokenFinalizer>();
    private List<ITokenFinalizer> _finalizersToRemove = new List<ITokenFinalizer>();
    private Stack<int> _newlineIndexes = new Stack<int>();
    private int _lineNumber = 0;

    public int CurrentLineNumber
    {
        get
        {
            return this._lineNumber + 1;
        }
    }

    // TODO: arguably, these should be PROTECTED/INTERNAL vs public. 
    public NewLineStatus NewLineStatus { get; set; }
    public StringStatus StringStatus { get; set; } = StringStatus.None;
    public BlockCommentStatus BlockCommentStatus { get; set; } = BlockCommentStatus.None;
    public CommentStatus CommentStatus { get; set; } = CommentStatus.None;
    public CharacterBuffer CharacterBuffer { get; set; } = new(10);

    public List<ICodeLine> CodeLines { get; internal set; } = new();
    public List<CodeString> Strings { get; internal set; } = new();
    public List<GoStatement> GoStatements { get; internal set; } = new();
    public List<BlockComment> BlockComments { get; internal set; } = new();
    public List<Comment> Comments { get; internal set; } = new();
    public int BlockCommentNestingLevel { get; set; }

    public string RawText { get; private set; } = string.Empty;
    public int CurrentIndex { get; private set; } = -1;

    public void EnlistInitializer(ITokenInitializer initializer)
    {
        this._tokenInitializers.Add(initializer);
    }

    public void EnlistFinalizer(ITokenFinalizer finalizer)
    {
        this._tokenFinalizers.Add(finalizer);
    }

    public void MarkFinalizerForRemoval(ITokenFinalizer finalizer)
    {
        this._finalizersToRemove.Add(finalizer);
    }

    public int GetCurrentLineStartOffset()
    {
        return this._newlineIndexes.Peek();
    }

    public int GetLineStartOffsetByOffset(int offset)
    {
        // TODO: https://overachieverllc.atlassian.net/browse/TSM-17
        int[] indexes = this._newlineIndexes.ToArray();

        if (indexes.Length == 0)
            return 1;

        int current = 0;
        int index = 0;
        while (offset >= current)
        {
            if (index >= indexes.Length)
                return current;

            current = indexes[index];
            index++;
        }

        return current;
    }

    public CodeLine GetCurrentLineFromCurrentLocation()
    {
        // REFACTOR: this really could/should just be a LINQ 'query' to find the 'index' closest to the offset (but not over it).

        int start = this.GetCurrentLineStartOffset();

        char[] chars = { '\r', '\n' };
        int end = this.RawText.IndexOfAny(chars, start);
        if (end == -1)
            end = this.RawText.Length;

        string currentLine = this.RawText.Substring(start, end - start);

        return new CodeLine(currentLine, start, end);
    }

    internal Tokenizer(string rawText)
    {
        this.RawText = rawText;
        this.Initialize();
    }

    protected void Initialize()
    {
        this.EnlistInitializer(new CrLfInitializer());
        this.EnlistInitializer(new StringInitializer());
        this.EnlistInitializer(new GoInitializer());
        this.EnlistInitializer(new BlockCommentInitializer());
        this.EnlistInitializer(new CommentInitializer());
    }

    // EVENTUALLY: public static Tokenizer StreamTokenizer(Stream stream) ... for perf reasons?

    public static Tokenizer StringTokenizer(string rawText)
    {
        return new Tokenizer(rawText);
    }

    public void Tokenize()
    {
        int readValue;
        this.CurrentIndex = 0;
        this._newlineIndexes.Push(0);

        using StringReader sr = new StringReader(this.RawText);
        while ((readValue = sr.Read()) != -1)
        {
            char current = (char)readValue;

            foreach (var finalizer in this._tokenFinalizers)
            {
                if (finalizer.WatchesFor(current))
                    finalizer.Process(this, sr, current);
            }

            foreach (var initializer in this._tokenInitializers)
            {
                if (initializer.Handles(current))
                    initializer.Process(this, sr, current);
            }

            foreach (var finalizer in this._finalizersToRemove)
            {
                finalizer.ProcessRemoval(this);
                if (this._tokenFinalizers.Contains(finalizer))
                    this._tokenFinalizers.Remove(finalizer);

                this._finalizersToRemove = new List<ITokenFinalizer>();
            }

            this.CharacterBuffer.Add(current);
            this.CurrentIndex++;
        }

        // finalizers get one last chance to terminate any non-completed (i.e., 'open') tokens:
        foreach (var finalizer in this._tokenFinalizers)
            finalizer.Terminate(this);

        int lastNewLineStart = this.GetCurrentLineStartOffset();
        if (this.CurrentIndex - lastNewLineStart > -1)
            this.AddCodeLineFromCurrentLocation();
    }

    public List<ParsedBatch> GetParsedBatches(bool ignoreGoInUseOnlyBatches = false)
    {
        List<ParsedBatch> output = new List<ParsedBatch>();

        int previousStart = 0;
        foreach (var go in this.GoStatements)
        {
            int end = go.OffsetEnd - previousStart - go.Text.Length;

            string batchText = this.RawText.Substring(previousStart, end).Trim();
            if (string.IsNullOrWhiteSpace(batchText) || batchText == go.Text)
                continue;

            var sources = new TextSources(this.RawText, this.RawText.Substring(previousStart, (end + go.Text.Length)));
            var batch = new ParsedBatch(previousStart, go.OffsetEnd, batchText, sources);
            batch.GoStatement = go;
            output.Add(batch);

            previousStart = go.OffsetStart + go.Text.Length;
        }

        if (previousStart < this.RawText.Length)
        {
            var batchText = this.RawText.Substring(previousStart, this.RawText.Length - previousStart).Trim();
            if (!string.IsNullOrWhiteSpace(batchText))
            {
                var sources = new TextSources(this.RawText, this.RawText.Substring(previousStart, this.RawText.Length - previousStart));
                var batch = new ParsedBatch(previousStart, this.RawText.Length, batchText, sources);
                output.Add(batch);
            }
        }

        if (ignoreGoInUseOnlyBatches)
        {
            var modifiedBatches = new List<ParsedBatch>();
            previousStart = 0;
            var sourceText = this.RawText;

            int currentBatch = 0;
            foreach (var batch in output)
            {
                currentBatch++;
                var text = batch.BatchText;

                int goLength = 0;
                if (batch.GoStatement != null)
                {
                    goLength = batch.GoStatement.Text.Length;

                    var regex = new Regex(@"(?<using>(\s*USE\s*\[.{1,255}?\]\s*;*|\s*USE\s+[^\[\s]{1,255}))", Global.SingleLineRegexOptions);
                    if (regex.IsMatch(text))
                    {
                        text = regex.Replace(text, "");

                        regex = new Regex(@"(?<comment>/\*.*?\*/)", Global.SingleLineRegexOptions);
                        text = regex.Replace(text, "");
                        regex = new Regex(@"--[^\r\n]*", Global.SingleLineRegexOptions);
                        text = regex.Replace(text, "");

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            sourceText = sourceText.ReplaceAtIndex(batch.GoStatement.OffsetStart, ' ');
                            sourceText = sourceText.ReplaceAtIndex(batch.GoStatement.OffsetStart + 1, ' ');

                            // There's an EDGE case where a 'batch' might terminate with a USE xxxx; ... and have NOTHING after it. 
                            //      that's ... useless, but, need to account for it (which the code below does):
                            if (currentBatch == output.Count)
                            {
                                var tailBatch = new ParsedBatch(previousStart, batch.EndIndex, batch.BatchText, batch.TextSources);
                                modifiedBatches.Add(tailBatch);
                            }

                            continue;
                        }
                    }
                }

                var newBatch = new ParsedBatch(previousStart, batch.EndIndex - goLength, sourceText.Substring(previousStart, batch.EndIndex - previousStart - goLength).Trim(), batch.TextSources);

                modifiedBatches.Add(newBatch);
                previousStart = batch.EndIndex;
            }

            output = modifiedBatches;
        }

        foreach (var batch in output)
        {
            batch.Comments = this.Comments.GetTokenByOffset(batch.StartIndex, batch.EndIndex);
            batch.BlockComments = this.BlockComments.GetTokenByOffset(batch.StartIndex, batch.EndIndex);
            batch.Strings = this.Strings.GetTokenByOffset(batch.StartIndex, batch.EndIndex);
        }

        // !!!! TODO: 
        //  after handling all bits of various formatting (well, except for the whole USE xxxx and GO replacement there)... 
        //      make sure that if a go.GoCount > 1 ... that ... I end up adding in a GO multiple times.... 

        return output;
    }

    public void AddCodeLineFromCurrentLocation()
    {
        int lineStart = this.GetCurrentLineStartOffset(); 
        int lineEnd = this.CurrentIndex + 1;
        if (lineEnd > this.RawText.Length)
            lineEnd = this.RawText.Length;

        string lineText = this.RawText.Substring(lineStart, lineEnd - lineStart);
        var codeLine = new CodeLine(lineText, lineStart, this.CurrentIndex + 1);

        codeLine.SetLineNumber(this.CurrentLineNumber);
        this.CodeLines.Add(codeLine);
        this._lineNumber++;

        this._newlineIndexes.Push(this.CurrentIndex + 1);
    }
}

public static class TokenizerExtensions
{
    public static List<T> GetTokenByOffset<T>(this List<T> tokens, int start, int end) where T : IToken<T>
    {
        var output = new List<T>();
        foreach (var token in tokens)
        {
            if (token.OffsetStart >= start && token.OffsetEnd <= end)
                output.Add(token.Clone());

            // TODO: The logic below lets us short-circuit once we've matched stuff overlapping start - end. BUT... it's NOT working - i.e., tests fail when 
            //      it's enabled/uncommented. Figure out what's up and/or if, honestly, it's needed (though, if there are 200 'strings' and we get what we need on 
            //         string #3 ...can't really see that it makes sense to go through the remaining 197 of them (i.e., i think it does make sense to try to get this to work).
            //if (end > token.OffsetEnd)
            //    break;
        }

        return output;
    }
}

public interface ITokenizerFactory
{
    Tokenizer FromString(string rawText);
    // Might make sense to implement this: Tokenizer FromStream(Stream stream);
}

public class TokenizerFactory : ITokenizerFactory {

    public Tokenizer FromString(string rawText)
    {
        return new Tokenizer(rawText);
    }
}