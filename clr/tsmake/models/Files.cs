﻿using System.Diagnostics;

namespace tsmake.models
{
    public class BuildFile
    {
        public List<IError> Errors { get; }
        public List<Line> Lines { get; private set; }
        // TODO: MIGHT make sense to add .Directives? 
        
        public RootPathDirective RootDirective { get; private set; }
        public OutputDirective OutputDirective { get; private set; }

        public BuildFile(string buildFile, IFileManager fileManager)
        {
            this.Errors = new List<IError>();

            this.ParseLines(buildFile, fileManager);
        }

        private void ParseLines(string source, IFileManager fileManager)
        {
            LinesProcessingResult result = LineProcessor.TransformLines(source, ProcessingType.BuildFile, fileManager, "", "");

            this.Errors.AddRange(result.Errors);
            this.Lines = result.Lines;

            // Bubble-up any Directives that need to be referenced (for simplicity) via BuildFile:
            var roots = result.Directives.Where(d => d.DirectiveName == "ROOT").ToList();
            switch (roots.Count)
            {
                case 0:
                    break;
                case 1:
                    this.RootDirective = (RootPathDirective)roots[0];
                    break;
                default:
                    string errorMessage = $"Duplicate ROOT Directive(s) in Build File: [{source}].";
                    string context = "ROOT directives on found on lines: " + string.Join(",",
                        roots.Select(r => r.Location.LineNumber.ToString()).ToArray());
                    this.Errors.Add(new ParserError(errorMessage, roots[0].Location, context));
                    break;
            }

            var outputs = result.Directives.Where(d => d.DirectiveName == "OUTPUT").ToList();
            switch (outputs.Count)
            {
                case 0:
                    break;
                case 1:
                    this.OutputDirective = (OutputDirective)outputs[0];
                    break;
                default:
                    string errorMessage = $"Duplicate OUTPUT Directive(s) in Build File: [{source}].";
                    string context = "OUTPUT directives on found on lines: " + string.Join(",",
                        outputs.Select(o => o.Location.LineNumber.ToString()).ToArray());
                    this.Errors.Add(new ParserError(errorMessage, outputs[0].Location, context));
                    break;       
            }
        }
    }

    public class BuildManifest
    {
        public List<Line> Lines { get; }

        public BuildManifest()
        {
            this.Lines = new List<Line>();
        }

        public void AddLine(Line line)
        {
            this.Lines.Add(line);
        }

        public void AddLines(List<Line> lines)
        {
            this.Lines.AddRange(lines);
        }
    }

    public interface IIncludeFile
    {
        public List<string> SourceFiles { get; }
        public List<IError> Errors { get; }
    }

    public class IncludedFile : IIncludeFile
    {
        public List<string> SourceFiles { get; }
        public List<IError> Errors { get; }

        public IncludedFile(IncludeFileDirective directive, IFileManager fileManager, string workingDirectory, string root)
        {
            this.Errors = new List<IError>();
            string translatedPath = fileManager.TranslatePath(directive.Path, directive.PathType, workingDirectory, root);

            if (!fileManager.FileExists(translatedPath))
            {
                this.Errors.Add(new ParserError("Include File xxx not found.", directive.Location, "context for where the file include directive was found and stuff... "));;
                this.SourceFiles = new List<string>();
            }
            else 
                this.SourceFiles = new List<string> { translatedPath };
        }
    }

    public class IncludedDirectory : IIncludeFile
    {
        private IFileManager FileManager { get; }
        public List<string> SourceFiles { get; }
        public List<IError> Errors { get; }
        public IncludeDirectoryDirective Directive { get; }

        public IncludedDirectory(IncludeDirectoryDirective directive, IFileManager manager, string workingDirectory, string root)
        {
            this.Errors = new List<IError>();
            this.SourceFiles = new List<string>();
            this.Directive = directive;

            this.FileManager = manager;
            this.ProcessFiles(workingDirectory, root);
        }

        private void ProcessFiles(string workingDirectory, string root)
        {
            List<string> priorities = new List<string>();
            List<string> normal = new List<string>();
            List<string> unpriorities = new List<string>();

            var translatedPath = this.FileManager.TranslatePath(this.Directive.Path, this.Directive.PathType, workingDirectory, root);
            if (!this.FileManager.DirectoryExists(translatedPath))
            {
                this.Errors.Add(new ParserError("Include Directory xxx not found.", this.Directive.Location, "context for where the file include directive was found and stuff... ")); ;
                return;
            }

            List<string> files = this.FileManager.GetDirectoryFiles(translatedPath, RecursionOption.TopOnly);

            foreach (string exclusionPattern in this.Directive.Exclusions)
            {
                var matches = files.Where(f => f.Like(exclusionPattern)).ToList();
                foreach (string match in matches)
                    files.Remove(match);
            }

            foreach (string priorityPattern in this.Directive.Priorities)
            {
                var matches = files.Where(f => f.Like(priorityPattern)).ToList();
                foreach (string match in matches)
                    files.Remove(match);

                priorities.AddRange(matches);
            }

            foreach (string unPriorityPattern in this.Directive.UnPriorities)
            {
                var matches = files.Where(f => f.Like(unPriorityPattern)).ToList();
                foreach (string match in matches)
                    files.Remove(match);

                unpriorities.AddRange(matches);
            }

            // TODO: need to implement options for sorting by create or modify date AND by DESC vs ASC... 
            //      which means I'm going to need either a silly case statement or some sort of func<> x...(that's loaded by a ... case statement). 
            //      AND, instead of pulling back MERELY the file-names (strings) of a given file via .GetDirectoryFiles()... i'm going to have to pull back 
            //      a 'struct' of name, mod-date, create-date so ... that I can sort on those attributes instead. 
            //  AND, the bummer - of course - is that this is going to make unit tests harder ... cuz ... fakes are going to require 3x entities vs 1x. 
            if (files.Count > 0)
                normal = files.OrderBy(f => f).ToList();

            // finally, assemble: 
            this.SourceFiles.AddRange(priorities);
            this.SourceFiles.AddRange(normal);
            this.SourceFiles.AddRange(unpriorities);
        }
    }

    public class IncludeFactory
    {
        public static IIncludeFile GetInclude(IDirective directive, IFileManager fileManager, string workingDirectory, string root)
        {
            if (directive.DirectiveName == "FILE")
                return new IncludedFile((IncludeFileDirective)directive, fileManager, workingDirectory, root);

            return new IncludedDirectory((IncludeDirectoryDirective)directive, fileManager, workingDirectory, root);
        }
    }

    public class OutputFileBuilder
    {
        // buffer/builder for final content - that'll be written to disk.
    }

    public class MarkerFile
    {
        // like the output file... but a 'builder'/buffer for the marker-file if used. 
    }

    public interface IFileManager
    {
        public string TranslatePath(string path, PathType pathType, string workingDirectory, string rootDirectory);
        public List<string> GetDirectoryFiles(string directory, RecursionOption recursion);
        public bool DirectoryExists(string path);
        public bool FileExists(string path);
        public List<string> GetFileLines(string filePath);
    }

    public class BaseFileManager : IFileManager
    {
        public string TranslatePath(string path, PathType pathType, string workingDirectory, string rootDirectory)
        {
            switch (pathType)
            {
                case PathType.Absolute:
                    return path;
                case PathType.Relative:
                    return workingDirectory.CollapsePath(path);
                case PathType.Rooted:
                    return rootDirectory.CollapsePath(path.Replace(@"\\\", ""));
                default:
                    return "";
            }
        }

        public List<string> GetDirectoryFiles(string directory, RecursionOption recursion)
        {
            SearchOption option = SearchOption.TopDirectoryOnly;
            if (recursion == RecursionOption.Recurse)
                option = SearchOption.TopDirectoryOnly;

            return Directory.GetFiles(directory, "*", option).ToList();
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public List<string> GetFileLines(string filePath)
        {
            List<string> output = new List<string>();
            string current;
            using (var streamReader = new StreamReader(filePath))
            {
                while ((current = streamReader.ReadLine()) != null)
                    output.Add(current);
            }

            return output;
        }
    }
}