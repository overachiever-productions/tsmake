namespace tsmake;

// TODO: change the name of this to a BuildManifest... 1) I've already got a -BuildFile in Posh...
//      (don't need another one) and 2) I'm already using 'manifest' for the lines. 
//          or... maybe an Assembler or ... something. 

public interface ISourceLine
{
    int LineNumber { get; }
    int Depth { get; }
    string FileName { get; }  // name of the current file. 
    Stack<string> Stack { get; }
    string LineText { get; }
}

public class SourceLine(int lineNumber, string fileName, string text, int depth, Stack<string> stack) : ISourceLine
{
    public int LineNumber { get; } = lineNumber;
    public int Depth { get; } = depth;              // REFACTOR: get this from the stack. 
    public string FileName { get; } = fileName;     // REFACTOR: pull this from the stack.   i.e., I don't need any kind of explicit inputs/etc. for this and previous. 
    public Stack<string> Stack { get; } = stack;
    public string LineText { get; } = text;
}

// TODO: create an interface... (for testing)
public class Manifest(IFileSystem fileSystem)
{
    private Stack<string> Stack = new Stack<string>();
    private IFileSystem FileSystem = fileSystem;
    
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
        foreach (string rawCodeLine in rawCodeLines)
        {
            // ALWAYS increment the line# - otherwise, we LOSE original line#s for reporting on problems/errors/etc. 
            lineNumber++; 

            if (DirectivesParser.IsCommentDirective(rawCodeLine))
                continue;

            if(DirectivesParser.IsRootDirective(rawCodeLine) || DirectivesParser.IsOutputDirective(rawCodeLine))
                continue;
            
            var currentLine = new SourceLine(lineNumber, filePath, rawCodeLine, depth, new Stack<string>(this.Stack));

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
            foreach (var line in rawCodeLines)
            {
                lineNumber++;
                
                if (DirectivesParser.IsIncludeDirective(line))
                {
                    var includeLine = new SourceLine(lineNumber, fullFilePath, line, depth, new Stack<string>(this.Stack));
                    var include = DirectivesParser.GetFileSystemDirective(includeLine, this.FileSystem);

                    foreach (var child in include.GetChildren())
                    {
                        List<ISourceLine> nestedManifestLines = RecurseSubFile(child, depth + 1);
                        output.AddRange(nestedManifestLines);
                    }
                }
                else 
                    output.Add(new SourceLine(lineNumber, fullFilePath, line, depth, new Stack<string>(this.Stack)));
            }

            return output;
        }
        catch 
        {
            throw;
        }
        finally
        {
            this.Stack.Pop();
        }
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
                var manifestLine = new SourceLine(lineNumber, filePath, line, 0, new Stack<string>(this.Stack));
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
