using tsmake.data_models;

namespace tsmake.workers;

public interface IDirectiveProcessor
{    
    List<IDirective> Directives { get; }

    void IdentifyDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<IError> syntaxErrors);

    void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<IError> syntaxErrors); 
}

public class DirectiveProcessor : IDirectiveProcessor
{
    public List<IDirective> Directives { get; } = new List<IDirective>();

    public void IdentifyDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<IError> syntaxErrors)
    {
        foreach (var codeLine in codeLines.Where(c => c.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && c.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2))
        {
            Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);

            if (!m.Success)
                continue;

            var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
            codeLine.Directive = directive;
            this.Directives.Add(directive);

            if (directive.IsValid)
            {
                if (directive is RootDirective rootDirective)
                    parent.BuildRoot.Add(new RankedString(SourceType.BuildFile, rootDirective.Payload));

                if (directive is OutputDirective outputDirective)
                    parent.BuildOutput.Add(new RankedString(SourceType.BuildFile, outputDirective.Payload));

                if (directive is FileMarkerDirective fileMarkerDirective)
                    parent.FileMarkerPath.Add(new RankedString(SourceType.BuildFile, fileMarkerDirective.Payload));
            }
            else
            {
                // REFACTOR: 98% sure that the FILENAME parameter for SyntaxError is redundant and unnecessary.  I can get it from the Stack<>.  So, remove it from the constructor and the interface.
                syntaxErrors.Add(new SyntaxError($"Invalid ##Directive: {directive.DirectiveName}.", directive.ValidationMessage, "sigh-file-name", codeLine.LineNumber, codeLine.Stack, ErrorSeverity.Fatal));
            }
        }
    }

    public void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<IError> syntaxErrors)
    {
        // CONVERT CONDITIONAL-FILE|DIRECTORY down to 'normal' FILE|DIRECTORY ... for processing down below. 
        foreach (var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveName == "Conditional-File") || (d.DirectiveName == "Conditional-Directory"))))
        {
            // convert these to FILE|DIRECTORY directives ... to be processed down below... 

            // e.g., directive(.type) = directive.ResolveCondition()
            // not EXACTLY sure how to swap out the current directive with a new one ... but ... yeah. 
        }

        // PROCESS CONDITIONAL directives
        foreach(var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveName.StartsWith("Condition")))))
        {
            // if these are BUILD-time directives ... then the ENTIRE CONDITION-BLOCK gets replaced with either CONDITION-DEFAULT or whichever CONDITION matched the evaluation. 

            // whereas if these are RUNTIME directives ... I need to use logic to generate dynamic SQL that'll be executed at runtime. 
            //    note that I've got a ROUGH implementation of this logic within my S4BuildPrototype. 
            //    the only wrinkle is ... how to map/create add MORE code-lines. 
            //    lol... maybe each 'codeline' is a code-snippet instead... 
            //      yeah... NO. think i just need a CodeLines.InsertAfter(currentLine)... 
        }

        foreach (var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveName == "File") || (d.DirectiveName == "Directory") || (d.DirectiveName == "Version-Checker"))))
        {
            IInclude include = IncludeTranslator.LoadInclude(directive, fileSystem, codeLines, syntaxErrors);
            foreach(string includedFilePath in include.IncludedFiles)
            {
                int lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile = directive.CodeLine.LineNumber;
                parent.RecursivelyAssemble(includedFilePath, lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile);
            }
        }
    }
}