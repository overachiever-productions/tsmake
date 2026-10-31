using tsmake.data_models;

namespace tsmake.workers;

public interface IDirectiveProcessor
{    
    List<IDirective> Directives { get; }

    void ProcessGlobalBuildFileDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines);
        
    void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ICodeLine> artifactLines, List<ISyntaxError> syntaxErrors); 
}

public class DirectiveProcessor : IDirectiveProcessor
{
    public List<IDirective> Directives { get; } = new List<IDirective>();

    public void ProcessGlobalBuildFileDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines)
    {
        var rootSet = false;
        var outputSet = false;
        var markerSet = false;

        foreach (var codeLine in currentLines.Where(c => (c.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && c.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2)))
        {
            Match m = Global.BuildDirectives.Match(codeLine.OriginalContent);

            if (!m.Success)
                continue;

            var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
            codeLine.Directive = directive;

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

            // TODO: 
            //  Add support for VERSION-CHECKER directive
            //   i.e., it's going to be 'build-file' level/scoped (can't be defined anywhere else)
            //   also ... make this a vNEXT feature ... I just don't care about it initially. 
        }
    }

    public void ProcessDirectives(IAssembler parent, IFileSystem fileSystem, List<ICodeLine> currentLines, List<ICodeLine> artifactLines, List<ISyntaxError> syntaxErrors)
    {
        ConditionDefaultDirective? currentConditionDirective = null;

        foreach (var codeLine in currentLines.Where(c => (c.OriginalContent.IndexOf("--", StringComparison.Ordinal) >= 0 && c.OriginalContent.IndexOf("##", StringComparison.Ordinal) >= 2)))
        {
            Match m = Global.DirectiveRegex.Match(codeLine.OriginalContent);

            if (!m.Success)
                continue;

            var directive = DirectiveParser.LoadDirective(m.Groups["directive"].Value, m.Groups["data"].Value, codeLine, fileSystem);
            codeLine.Directive = directive;

            if (!directive.IsValid)
                // TODO: "hmmm:" for the DETAIL part of this syntax error seems lame. 
                // I MIGHT need a signature for SyntaxError that REQUIRES this ... and ... probably do. 
                // BUT I also - clearly - don't always need this. 
                // MORE IMPORTANTLY, I MIGHT want to have some sort of 'source' parameter for ALL syntax errors - that tells me, roughly, where they came from WITHIN the code to make debugging and other fixes/improvements over the long-haul easier to track down. 
                syntaxErrors.Add(new SyntaxError(directive.ValidationMessage, "hmmmm", codeLine.LineNumber, codeLine.FileDetail));
            else 
                this.Directives.Add(directive);

            // TODO: need to add switch/cases for FLAG-DEFAULT, FLAG[s](), FLAG-END... virtually no different than CONDITION-DEFAULT, CONDITION[s](), CONDITION-END ... 
            switch (directive.DirectiveType)
            {
                case DirectiveType.ConditionDefault:
                    if (currentConditionDirective != null)
                    {
                        // oops this is a new condition ... inside of a non-closed condition. 
                        // syntax error that's fatal... (can't continue the build).
                    }
                    currentConditionDirective = (ConditionDefaultDirective)directive;
                    break;
                case DirectiveType.Condition:
                    if(currentConditionDirective == null)
                    {
                        // oops this is a condition ... outside of a default condition. 
                        // syntax error that's fatal... (can't continue the build).
                    }
                    else
                        currentConditionDirective.AddCondition((ConditionDirective)directive);
                    
                    break;
                case DirectiveType.ConditionEnd:
                    if(currentConditionDirective == null)
                    {
                        // oops this is a condition end ... outside of a default condition. 
                        // syntax error that's fatal... (can't continue the build).
                    }
                    else
                        currentConditionDirective.AddConditionEnd((ConditionEndDirective)directive);

                    currentConditionDirective = null;  // close the current condition... 
                    break;
                default:
                    break;
            }
        }

        if (currentConditionDirective != null)
        {
            // need to check if it's completed or not 
            // and if not: that's a syntax because the condition was never closed.
        }

        for (int i = 0; i < this.Directives.Count; i++)
        {
            // CONVERT FLAG-FILE|DIRECTORY FILE|DIRECTORY (or nothing/empty) ... depending upon #Flags.
            var directive = this.Directives[i];

            if (directive.IsValid && ((directive.DirectiveType == DirectiveType.FlagFile) || (directive.DirectiveType == DirectiveType.FlagDirectory)))
            {
                IFlagIncludeResolver resolver = (IFlagIncludeResolver)directive;

                FlagIncludeOption? option = resolver.ResolveOptionFromFlags(parent.Options.Flags);
                if (option != null)
                {
                    // doh... this needs to be forced to .IsValid = 1. er. no. that should already be true. 
                    //    assuming that the path is correct. 
                    this.Directives[i] = new FileDirective(option.Path, directive.CodeLine, fileSystem);

                    syntaxErrors.Add(new SyntaxError($"Directive type: {this.Directives[i].DirectiveType}", "||", directive.CodeLine));
                }
            }
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
                            syntaxErrors.Add(new SyntaxError("doing a line (well... file)", "||", line.LineNumber, line.FileDetail));
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
                        case DirectiveType.FlagDirectory:
                        case DirectiveType.FlagFile:
                            // do NOTHING... i.e.,  don't add to artifact lines - this is/was a FLAG-INCLUDE that is now empty/non-matched.
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