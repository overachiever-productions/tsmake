namespace tsmake;

public static class StackExtensions
{
    public static string PrintStack(this Stack<string> stack)
    {
        var copyOfStack = new Stack<string>(stack);
        StringBuilder builder = new StringBuilder();
        int depth = 0;
        while (copyOfStack.Count > 0)
        {
            if (depth == 0)
                builder.AppendLine(copyOfStack.Pop());
            else
                builder.AppendLine($"\t  -> {copyOfStack.Pop()}"); // I MIGHT want to put ⇗ in here instead of 'in' (or some other unicode 'arrow' thingy?)

            depth++;
        }

        return builder.ToString().TrimEnd();
    }
}

public interface ISourceLine
{
    // TODO: I honestly don't think I need .Start and .End. 
    //        1. If I do, they should be called Offsets. ... 
    //          only.
    //        2. If i call them offsets, they're NOT offsets - because they're 'offsets' with the CR | LF | CRLF removed. 
    //              which is pointless. 
    //    instead, all'z I think I need is: 
    //          .LineNumber  -> from this I can get the ACTUAL offsets via Tokenized Files. 
    //          .FileName
    //          .Stack
    //          .Text
    //int Start { get; }
    //int End { get; }
    int LineNumber { get; }
    int Depth { get; }
    string FileName { get; }  // name of the current file. 
    Stack<string> Stack { get; }
    string LineText { get; }

    //string PrintStack();
}

public class SourceLine(int lineNumber, string text, Stack<string> stack) : ISourceLine
{
    //public int Start { get; } = start;
    //public int End { get; } = end;
    public int LineNumber { get; } = lineNumber;
    public int Depth => this.Stack.Count;
    public string FileName => this.Stack.Peek(); 
    public Stack<string> Stack { get; } = stack;
    public string LineText { get; } = text;
}

public class Assembler(IFileSystem fileSystem, ITokenizerFactory tokenizerFactory)
{
    private Stack<string> Stack = new Stack<string>();
    private IFileSystem FileSystem = fileSystem;
    private ITokenizerFactory TokenizerFactory = tokenizerFactory;
    
    public RootDirective RootDirective { get; private set; } 
    public OutputDirective OutputDirective { get; private set; }

    public List<ISourceLine> CodeLines { get; } = new List<ISourceLine>();

    public void LoadContents(string filePath, int depth = 0)
    {
        List<string> rawCodeLines = this.FileSystem.GetFileLines(filePath);

        // REFACTOR: depth is ALWAYS == 0 ... cuz I wrote this func thinking it'd be the recursion func... then ... handed recursion OFF to the RecurseSubFile func... 
        if (depth == 0)
        {
            // TODO: validate the file path - i.e., make sure it's good or ... throw an exception/whatever. 

            this.ProcessCoreDirectives(rawCodeLines, filePath);
            if (this.RootDirective != null)
                this.FileSystem.SetRootDirectory(this.RootDirective.AbsolutePath);
            else
                this.FileSystem.SetRootDirectory(this.FileSystem.WorkingDirectory);
        }

        this.Stack.Push(filePath);
        
        int lineNumber = 0;
        foreach (string rawCodeLine in rawCodeLines)
        {
            lineNumber++;

            if (DirectivesParser.IsCommentDirective(rawCodeLine))
                continue;

            if(DirectivesParser.IsRootDirective(rawCodeLine) || DirectivesParser.IsOutputDirective(rawCodeLine))
                continue;
            
            var currentLine = new SourceLine(lineNumber, rawCodeLine, new Stack<string>(this.Stack));

            if (DirectivesParser.IsIncludeDirective(rawCodeLine))
            {
                var include = DirectivesParser.GetFileSystemDirective(currentLine, this.FileSystem);

                foreach (var child in include.GetChildren())
                {
                    var manifestLines = RecurseSubFile(child, depth + 1);
                    foreach (var line in manifestLines)
                    {
                        // TODO: if it's illegal (i.e., an illegal directive)... ignore or throw...  (probably ignore. I don't care about missed directives)
                        //          and 'illegal' here (for a directive) might mean something like ROOT, OUTPUT or whatever (i.e., within a NESTED/SUB-FILE).

                        // if it's a comment ... don't add. 
                        if(DirectivesParser.IsCommentDirective(line.LineText))
                            continue;

                        // otherwise:
                        this.CodeLines.Add(line);
                    }
                }
            }
            else
                this.CodeLines.Add(currentLine);
        }
    }

    private List<ISourceLine> RecurseSubFile(string fullFilePath, int depth)
    {
        var output = new List<ISourceLine>();

        try
        {
            this.Stack.Push(fullFilePath);

            List<string> rawCodeLines = this.FileSystem.GetFileLines(fullFilePath);

            int lineNumber = 0;
            foreach (var line in rawCodeLines)
            {
                lineNumber++;
                
                if (DirectivesParser.IsIncludeDirective(line))
                {
                    var includeLine = new SourceLine(lineNumber, line, new Stack<string>(this.Stack));
                    var include = DirectivesParser.GetFileSystemDirective(includeLine, this.FileSystem);

                    foreach (var child in include.GetChildren())
                    {
                        List<ISourceLine> nestedManifestLines = RecurseSubFile(child, depth + 1);
                        output.AddRange(nestedManifestLines);
                    }
                }
                else 
                    output.Add(new SourceLine(lineNumber, line, new Stack<string>(this.Stack)));

                string fileContents = this.FileSystem.GetFileContent(fullFilePath);
                var tokenizer = this.TokenizerFactory.FromString(fileContents);

                tokenizer.Tokenize(); // we're NOT interested in tokenized results - just checking that we DON'T have an open strings/comments... 
            }
        }
        catch (SyntaxException sex)
        {
            throw new SyntaxException(sex.Message, sex.LineNumber, sex.LineOffsetStart, sex.OffsetStart, sex.OffsetEnd, new Stack<string>(this.Stack));
        }
        catch 
        {
            throw;  // preserve stack-trace
        }
        finally
        {
            this.Stack.Pop();
        }

        return output;
    }

    private void ProcessCoreDirectives(List<string> rawCodeLines, string filePath)
    {
        bool rooted = false;
        bool outputed = false;

        int lineNumber = 0;
        foreach (string line in rawCodeLines)
        {
            lineNumber++; // ALWAYS increments... 

            if (DirectivesParser.IsRootDirective(line))
            {
                var manifestLine = new SourceLine(lineNumber, line, new Stack<string>(this.Stack));
                this.RootDirective = (RootDirective)DirectivesParser.GetFileSystemDirective(manifestLine, this.FileSystem);

                rooted = true;
            }

            if (DirectivesParser.IsOutputDirective(line))
                outputed = true;

            if (rooted && outputed)
                return;
        }
    }
}
