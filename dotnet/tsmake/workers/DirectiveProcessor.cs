using tsmake.data_models;

namespace tsmake.workers;

public interface IDirectiveProcessor
{    
    List<IDirective> Directives { get; }

    void ProcessGlobalBuildFileDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ISyntaxError> syntaxErrors);
        
    void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ICodeLine> artifactLines, List<ISyntaxError> syntaxErrors); 
}

public class DirectiveProcessor : IDirectiveProcessor
{
    public List<IDirective> Directives { get; } = new List<IDirective>();

    // REFACTOR: this is a temporary name while I sort out logic... and BEFORE I totally rewrite this logic.
    public void ProcessGlobalBuildFileDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ISyntaxError> syntaxErrors)
    {
        var rootSet = false;
        var outputSet = false;
        var markerSet = false;

        foreach (var codeLine in currentLines.Where(c => (c.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && c.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2)))
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

    public void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ICodeLine> artifactLines, List<ISyntaxError> syntaxErrors)
    {
        // IDENTIFY directives: 
        foreach (var codeLine in currentLines.Where(c => (c.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && c.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2)))
        {
            Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);

            if (!m.Success)
                continue;

            var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
            codeLine.Directive = directive;
            this.Directives.Add(directive);
        }
        
        foreach (var directive in this.Directives.Where(d => d.IsValid && ((d.DirectiveType == DirectiveType.ConditionalFile) || (d.DirectiveType == DirectiveType.ConditionalDirectory))))
        {
            // CONVERT CONDITIONAL-FILE|DIRECTORY down to 'normal' FILE|DIRECTORY ... for processing down below. 

            // e.g., directive(.type) = directive.ResolveCondition()
            // not EXACTLY sure how to swap out the current directive with a new one ... but ... yeah. 
            //   BUT ... this has to be done at the CodeLine level ... AND ... i need to 'swap out' whatever .directive I'm currently working on/against with the FILE or DIRECTORY that replaces it. 
        }

        foreach (ICodeLine line in currentLines)
        {
            if(line.Directive != null)
            {
                if(line.Directive.IsValid)
                {
                    switch(line.Directive.DirectiveType)
                    {
                        case DirectiveType.File:
                        case DirectiveType.Directory:
                            IInclude include = IncludeTranslator.LoadInclude(line.Directive, fileSystem, currentLines, syntaxErrors);
                            foreach (string includedFilePath in include.IncludedFiles)
                            {
                                int lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile = line.LineNumber;
                                parent.RecursivelyAssemble(includedFilePath, lineOfCurrentFileThatDirectedTheInclusionOfNewChildFile);
                            }
                            break;
                        case DirectiveType.ConditionDefault:
                            // IF the Directive is a BUILD-TIME directive ... 
                            //          the process the CASEs and: 
                            //          for the entire CASE that matches ... 
                            //                  for EACH line in the CASE ... add to artifactLines.
                            //        at this point ... we've swapped ##directives and #conditions and #end-condition ... with ... code-lines to be output. 
                            //          AND ... remove /* */ or -- from the 'case' in question ... so that the code-lines are 'active' and not commented-out.

                            // IF the directive is a RUNTIME directive... 
                            //      hand off to a FUNC that'll generate the dynamic SQL and so on... 
                            //      which'll hand-back a LIST<ICodeLine> ... (that'll be 'de-commented')
                            //     and then ... foreach of those lines ... add to artifactLines.
                            break;
                        default:
                            // for everything that does NOT match (e.g., ROOT, OUTPUT,  COMMENT, CONDITION, CONDITION-END, ETC...) simply SKIP the line... 
                            break;
                    }
                }
            }
            else
                artifactLines.Add(line);
        }
    }
}