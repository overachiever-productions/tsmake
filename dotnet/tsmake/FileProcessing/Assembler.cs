namespace tsmake;

public interface ISourceLine
{
    int Start { get; }
    int End { get; }
    int LineNumber { get; }
    int Depth { get; }
    string FileName { get; }  // name of the current file. 
    Stack<string> Stack { get; }
    string LineText { get; }

    string PrintStack();
}

public class SourceLine(int start, int end, int lineNumber, string text, Stack<string> stack) : ISourceLine
{
    public int Start { get; } = start;
    public int End { get; } = end;
    public int LineNumber { get; } = lineNumber;
    public int Depth => this.Stack.Count;
    public string FileName => this.Stack.Peek(); 
    public Stack<string> Stack { get; } = stack;
    public string LineText { get; } = text;

    public string PrintStack()
    {
        // Eventually, I want this to look a bit more like this: 
        //      D:\\FakeDir\\SomeFile.sql:line x(18 - 24)
        //      	in D:\\FakeDir\\ParentFile.build.sql: line y
        //      	in D:\\FakeDir\\my_latest.build.sql: line z

        var copyOfStack = new Stack<string>(this.Stack);
        StringBuilder builder = new StringBuilder();
        int depth = 0;
        while (copyOfStack.Count > 0)
        {
            if(depth == 0)
                builder.AppendLine(copyOfStack.Pop());
            else
                
                builder.AppendLine($"\tin {copyOfStack.Pop()}");

            depth++;
        }

        return builder.ToString().TrimEnd();
    }
}

// TODO: create an interface... (for testing)
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
        int offset = 1;
        foreach (string rawCodeLine in rawCodeLines)
        {
            lineNumber++;
            var start = offset;
            var end = offset + rawCodeLine.Length;
            offset = end;


            // TODO: 
            // HMMM. 
            // I could have an int position = 0; right up near the lineNumber = 0 declaration. 
            // and here, for every, single, line that gets processed, get the LENGTH() of the line itself
            //      and the START of each codeline would then be position, and the END of each code-line would then be position + LENGTH()
            //      and then set position = position + LENGTH()... 
            // and ... that now the start/end (or offset) of each code line.
            //   then, if I ever want/need to lookup a code LINE by its position within the file... 
            //      i do a while(targetPosition < startOfLine)
            //          or whatever ... so that I basically zip through (foreach) EACH CodeLine
            //                      in a given file until I find a .Start > targetPosition... at which point, i know which line i'm on... 
            //                      and... done. 
            //      the above ALL presupposes that each "rawCodeLine" i'm iterating through HAS the CRLF, LF, or CR as part of the line in question... 
            //          if that's NOT true... then I've got some issues. 
            //      also, not quite sure why I couldn't do this via the tokenizer too... 
            //      though, I guess that comes later in the pipeline. 


            if (DirectivesParser.IsCommentDirective(rawCodeLine))
                continue;

            if(DirectivesParser.IsRootDirective(rawCodeLine) || DirectivesParser.IsOutputDirective(rawCodeLine))
                continue;
            
            var currentLine = new SourceLine(start, end, lineNumber, rawCodeLine, new Stack<string>(this.Stack));

            if (DirectivesParser.IsIncludeDirective(rawCodeLine))
            {
                var include = DirectivesParser.GetFileSystemDirective(currentLine, this.FileSystem);

                foreach (var child in include.GetChildren())
                {
                    var manifestLines = RecurseSubFile(child, depth + 1);
                    foreach (var line in manifestLines)
                    {
                        // if it's illegal (i.e., an illegal directive)... ignore or throw...  (probably ignore. I don't care about missed directives)
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
        this.Stack.Push(fullFilePath);
        var output = new List<ISourceLine>();

        try
        {
            List<string> rawCodeLines = this.FileSystem.GetFileLines(fullFilePath);

            int lineNumber = 0;
            int offset = 1;
            foreach (var line in rawCodeLines)
            {
                lineNumber++;
                var start = offset;
                var end = offset + line.Length;
                offset = end;
                
                if (DirectivesParser.IsIncludeDirective(line))
                {
                    var includeLine = new SourceLine(start, end, lineNumber, line, new Stack<string>(this.Stack));
                    var include = DirectivesParser.GetFileSystemDirective(includeLine, this.FileSystem);

                    foreach (var child in include.GetChildren())
                    {
                        List<ISourceLine> nestedManifestLines = RecurseSubFile(child, depth + 1);
                        output.AddRange(nestedManifestLines);
                    }
                }
                else 
                    output.Add(new SourceLine(start, end, lineNumber, line, new Stack<string>(this.Stack)));
            }
        }
        catch 
        {
            throw;
        }
        finally
        {
            this.Stack.Pop();
        }

        string fileContents = this.FileSystem.GetFileContent(fullFilePath);
        var tokenizer = this.TokenizerFactory.FromString(fileContents);
        
        // TODO: wrap this in a try catch and/or put SOME sort of error handling here. 
        tokenizer.Tokenize();

        return output;
    }

    private void ProcessCoreDirectives(List<string> rawCodeLines, string filePath)
    {
        bool rooted = false;
        bool outputed = false;

        int lineNumber = 0;
        int offset = 1;
        foreach (string line in rawCodeLines)
        {
            lineNumber++; // ALWAYS increments... 
            var start = offset;
            var end = offset + line.Length;
            offset = end;

            if (DirectivesParser.IsRootDirective(line))
            {
                var manifestLine = new SourceLine(start, end, lineNumber, line, new Stack<string>(this.Stack));
                this.RootDirective = (RootDirective)DirectivesParser.GetFileSystemDirective(manifestLine, this.FileSystem);

                rooted = true;
            }

            if (DirectivesParser.IsOutputDirective(line))
            {

                outputed = true;
            }

            if (rooted && outputed)
                return;

        }
    }
}
