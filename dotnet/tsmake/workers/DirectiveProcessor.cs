using tsmake.data_models;

namespace tsmake.workers;

public interface IDirectiveProcessor
{    
    List<IDirective> Directives { get; }

    void IdentifyDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors);
        
    void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors); 
}

public class DirectiveProcessor : IDirectiveProcessor
{
    public List<IDirective> Directives { get; } = new List<IDirective>();

    public void IdentifyDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
    {
        // HACK: ... this is just ugly. need to refactor and make some model responsible for managing these details and preventing duplicates. 
        //          ALSO... it's 'Exponentially ugly' - in that each new INCLUDE file ... resets this ... so, ... yeah: terrible.
        var rootSet = false;
        var outputSet = false;
        var markerSet = false;

        foreach(var codeLine in codeLines.Where(c => !c.Processed))
        {
            codeLine.Processed = true;

            if (codeLine.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && codeLine.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2)
            {
                Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);

                if (!m.Success)
                    continue;

                var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
                codeLine.Directive = directive;
                this.Directives.Add(directive);

                if (directive is RootDirective rootDirective)
                {
                    if (rootSet)
                    {
                        parent.ConfigErrors.Add(new ConfigError($"Duplicate ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.BuildRoot));
                        continue;
                    }

                    rootSet = true;
                    if (rootDirective.IsValid)
                        parent.BuildRoot.Add(new RankedString(SourceType.BuildFile, rootDirective.Payload, directive));
                    else
                        parent.ConfigErrors.Add(new ConfigError($"Invalid ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.BuildRoot));
                }

                if (directive is OutputDirective outputDirective)
                {
                    if (outputSet)
                    {
                        parent.ConfigErrors.Add(new ConfigError($"Duplicate ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.BuildOutput));
                        continue;
                    }

                    outputSet = true;
                    if (outputDirective.IsValid)
                        parent.BuildOutput.Add(new RankedString(SourceType.BuildFile, outputDirective.Payload, directive));
                    else
                        parent.ConfigErrors.Add(new ConfigError($"Invalid ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.BuildOutput));
                }

                if (directive is FileMarkerDirective fileMarkerDirective)
                {
                    if (fileMarkerDirective.IsValid)
                    {
                        if (!markerSet)
                        {
                            parent.FileMarkerPath.Add(new RankedString(SourceType.BuildFile, fileMarkerDirective.Payload, directive));
                            markerSet = true;
                        }
                        else
                            parent.ConfigErrors.Add(new ConfigError($"Duplicate ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.FileMarkerPath));
                    }
                    else
                        parent.ConfigErrors.Add(new ConfigError($"Invalid ##{directive.DirectiveType} directive.", directive.ValidationMessage, parent.FileMarkerPath));
                }
            }
        }
    }

    public void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> codeLines, List<ISyntaxError> syntaxErrors)
    {
        // CONVERT CONDITIONAL-FILE|DIRECTORY down to 'normal' FILE|DIRECTORY ... for processing down below. 
        foreach (var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveType == DirectiveType.ConditionalFile) || (d.DirectiveType == DirectiveType.ConditionalDirectory))))
        {
            // convert these to FILE|DIRECTORY directives ... to be processed down below... 

            // e.g., directive(.type) = directive.ResolveCondition()
            // not EXACTLY sure how to swap out the current directive with a new one ... but ... yeah. 
        }

        // PROCESS CONDITIONAL directives
        foreach(var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveType == DirectiveType.Condition) || (d.DirectiveType == DirectiveType.ConditionDefault) || (d.DirectiveType == DirectiveType.ConditionEnd))))
        {
            // if these are BUILD-time directives ... then the ENTIRE CONDITION-BLOCK gets replaced with either CONDITION-DEFAULT or whichever CONDITION matched the evaluation. 

            // whereas if these are RUNTIME directives ... I need to use logic to generate dynamic SQL that'll be executed at runtime. 
            //    note that I've got a ROUGH implementation of this logic within my S4BuildPrototype. 
            //    the only wrinkle is ... how to map/create add MORE code-lines. 
            //      Think i just need a CodeLines.InsertAfter(currentLine)... 


            // SPECIFICALLY, the process needs to be: 
            //  1. find the index of the current line (note that the INDEX is no longer guaranteed to be the current 'LINE NUMBER' ... cuz we're inserting rows/etc. 
            //      this can be done with, effectively, a .FindIndex() on the CodeLines collection.
            //              e.g., int currentLineIndex = codeLines.FindIndex(c => c.LineNumber == directive.CodeLine.LineNumber);

            //  2. find the index of the END-CONDITION directive ... which will be the last line of the block.

            //  3. remove all lines from the current line index to the END-CONDITION directive index.

            //  4. insert the new lines (either CONDITION-DEFAULT or whichever CONDITION matched the evaluation) at the current line index.
            //      e.g., codeLines.InsertRange(currentLineIndex, newLines);

        }

        foreach (var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveType == DirectiveType.File) || (d.DirectiveType == DirectiveType.Directory) || (d.DirectiveType == DirectiveType.VersionChecker))))
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