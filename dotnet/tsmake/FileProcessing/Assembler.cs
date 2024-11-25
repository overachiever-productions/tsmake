namespace tsmake;

public static class StackExtensions
{
    // REFACTOR: ... actually, I could probably make this an EXTENSION of my own EXCEPTIONs (SyntaxException) and so on
    //      and then have the .PrintStack do all of the 'work' of burrowing down into exceptions' .ISourceLine -> .Stack. 
    //  that'd be a LOT easier to call from within POWERSHELL. 
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
    int LineNumber { get; }
    int Depth { get; }
    string FileName { get; }  // name of the current file. 
    Stack<string> Stack { get; }
    string LineText { get; }
}

public class SourceLine(int lineNumber, string text, Stack<string> stack) : ISourceLine
{
    public int LineNumber { get; } = lineNumber;
    public int Depth => this.Stack.Count;
    public string FileName => this.Stack.Peek(); 
    public Stack<string> Stack { get; } = stack;
    public string LineText { get; } = text;
}

public class Assembler(IFileSystem fileSystem, ITokenizerFactory tokenizerFactory)
{
    private ISourceLine _currentSourceLine;
    private Stack<string> Stack = new Stack<string>();
    private IFileSystem FileSystem = fileSystem;
    private ITokenizerFactory TokenizerFactory = tokenizerFactory;
    
    public RootDirective RootDirective { get; private set; } 
    public OutputDirective OutputDirective { get; private set; }

    public List<ISourceLine> CodeLines { get; } = new List<ISourceLine>();

    public void LoadContents(string filePath)
    {
        // TODO: validate the file path - i.e., make sure it's good or ... throw an exception/whatever. 
        //      actually... just make sure that this is done from within powershell ... 

        List<string> rawCodeLines = this.FileSystem.GetFileLines(filePath);

        this.ProcessCoreDirectives(rawCodeLines, filePath);
        this.FileSystem.SetRootDirectory(this.RootDirective == null ? this.FileSystem.WorkingDirectory : this.RootDirective.AbsolutePath);

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
                    var manifestLines = RecurseSubFile(child);
                    foreach (var line in manifestLines)
                    {
                        // TODO: if it's illegal (i.e., an illegal directive)... ignore or throw...  (probably ignore. I don't care about missed directives)
                        //          and 'illegal' here (for a directive) might mean something like ROOT, OUTPUT or whatever (i.e., within a NESTED/SUB-FILE).

                        if(DirectivesParser.IsCommentDirective(line.LineText))
                            continue;

                        this.CodeLines.Add(line);
                    }
                }
            }
            else
                this.CodeLines.Add(currentLine);
        }
    }

    private List<ISourceLine> RecurseSubFile(string fullFilePath)
    {
        var output = new List<ISourceLine>();

        try
        {
            // TODO: make sur the file EXISTS before attempting to push it into the stack. 
            //  MIGHT even make sense to do this ... from within the caller... (which is self after a point, but is initially .LoadContents()).

            this.Stack.Push(fullFilePath);

            List<string> rawCodeLines = this.FileSystem.GetFileLines(fullFilePath);

            int lineNumber = 0;
            foreach (var line in rawCodeLines)
            {
                lineNumber++;

                this._currentSourceLine = new SourceLine(lineNumber, line, new Stack<string>(this.Stack));

                if (DirectivesParser.IsIncludeDirective(line))
                {
                    var include = DirectivesParser.GetFileSystemDirective(this._currentSourceLine, this.FileSystem);

                    foreach (var child in include.GetChildren())
                    {
                        List<ISourceLine> nestedManifestLines = RecurseSubFile(child);
                        output.AddRange(nestedManifestLines);
                    }
                }
                else 
                    output.Add(this._currentSourceLine);

                string fileContents = this.FileSystem.GetFileContent(fullFilePath);
                var tokenizer = this.TokenizerFactory.FromString(fileContents);

                tokenizer.Tokenize(); // we're NOT interested in tokenized results - just checking that we DON'T have an open strings/comments... 
            }
        }
        catch (SyntaxException sex)
        {
            throw new SyntaxException(sex.Message, sex.LineNumber, sex.LineOffsetStart, sex.OffsetStart, sex.OffsetEnd, this._currentSourceLine);
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