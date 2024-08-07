﻿namespace tsmake.models
{
    public class CodeFile
    {
        public string FileName { get; }
        public string FileBody { get; }

        public CodeFile(string fileName, string fileBody)
        {
            this.FileName = fileName;
            this.FileBody = fileBody;
        }
    }
    
    public class BuildFile
    {
        public List<IError> Errors { get; }
        public List<Line> Lines { get; private set; }
        public List<IDirective> Directives { get; private set; }
        public List<Token> Tokens { get; private set; }

        public RootDirective RootDirective { get; private set; }
        public OutputDirective OutputDirective { get; private set; }

        public BuildFile(string buildFile, IFileManager fileManager)
        {
            this.Errors = new List<IError>();

            this.ParseLines(buildFile, fileManager);
        }

        private void ParseLines(string buildFileSource, IFileManager fileManager)
        {
            Stack<Location> empty = new Stack<Location>();
            FileProcessingResult result = FileProcessor.ProcessFileLines(null, buildFileSource, ProcessingType.BuildFile, fileManager, "", "");

            this.Errors.AddRange(result.Errors);
            this.Lines = result.Lines;
            this.Directives = result.Directives;

            // Bubble-up any Directives that need to be referenced (for simplicity) via BuildFile:
            var roots = result.Directives.Where(d => d.DirectiveName == "ROOT").ToList();
            switch (roots.Count)
            {
                case 0:
                    break;
                case 1:
                    this.RootDirective = (RootDirective)roots[0];
                    break;
                default:
                    string errorMessage = $"Duplicate ROOT Directive(s) in Build File: [{buildFileSource}].";
                    string context = "ROOT directives on found on lines: " + string.Join(",",
                        roots.Select(r => r.Location.Peek().LineNumber.ToString()).ToArray());
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
                    string errorMessage = $"Duplicate OUTPUT Directive(s) in Build File: [{buildFileSource}].";
                    string context = "OUTPUT directives on found on lines: " + string.Join(",",
                        outputs.Select(o => o.Location.Peek().LineNumber.ToString()).ToArray());
                    this.Errors.Add(new ParserError(errorMessage, outputs[0].Location, context));
                    break;       
            }
        }
    }

    public class BuildManifest
    {
        public List<Line> Lines { get; }
        public List<IError> Errors { get; }
        public List<Token> Tokens { get; set; }
        public List<IDirective> Directives { get; }
        public List<CodeComment> Comments { get; private set; }
        public List<CodeString> CodeStrings { get; private set; }

        public BuildManifest(BuildFile buildFile)
        {
            this.Lines = new List<Line>();
            this.Errors = new List<IError>(); 

            this.Tokens = new List<Token>();
            this.Directives = new List<IDirective>();
            this.Directives.AddRange(buildFile.Directives);
            this.Comments = new List<CodeComment>();
            this.CodeStrings = new List<CodeString>();
        }

        public void AddLine(Line line)
        {
            this.Lines.Add(line);

            if (line.Directive != null)
            {
                if(!this.Directives.Contains(line.Directive))
                    this.Directives.Add(line.Directive);
            }

            foreach (var token in line.Tokens)
            {
                if(!this.Tokens.Contains(token))
                    this.Tokens.Add(token);
            }
            
            foreach (var comment in line.CodeComments)
            {
                if(!this.Comments.Contains(comment))
                    this.Comments.Add(comment);
            }

            foreach (var codeString in line.CodeStrings)
            {
                if(!this.CodeStrings.Contains(codeString))
                    this.CodeStrings.Add(codeString);
            }
        }

        public void AddLines(FileProcessingResult result)
        {
            foreach(var line in result.Lines)
                this.AddLine(line);

            foreach (var directive in result.Directives)
            {
                if(!this.Directives.Contains(directive))
                    this.Directives.Add(directive);
            }

            foreach (var token in result.Tokens)
            {
                if (!this.Tokens.Contains(token))
                    this.Tokens.Add(token);
            }

            foreach (var comment in result.Comments)
            {
                if (!this.Comments.Contains(comment))
                    this.Comments.Add(comment);
            }

            foreach (var codeString in result.CodeStrings)
            {
                if (!this.CodeStrings.Contains(codeString))
                    this.CodeStrings.Add(codeString);
            }
        }

        public void IdentifyObjects()
        {

            // Reference: 
            //      D:\Dropbox\Repositories\tsmake\~~spelunking\~~SYNTAX DETAILS.sql



            // this'll actually grab up to 3-part names in CREATE OR ALTER statements:
            // @"\s*CREATE\s*OR\s*ALTER\s*(?<object>\w+)\s+(?<identifier>\w+\.*\w+\.+\w*)"


            // ORDER OF OPERATIONS HERE IS: 
            //  1) look for CREATE OR ALTER ... those are the MOST specific. 
            //  2) if NOT a match for above... 
            //          then check for ALTER|CREATE (and possibly DROP - but probably not? cuz... a) if it shows up, it might be an IF EXISTS... DROP and b) I don't care about DROPs, really - any .DOCUMENTATION that exists won't be for a DROP - only an ALTER or a CREATE... 
            //      a) check for .. simple/1-part OBJECT-NAMEs
            //      b) check for 'compound' object type names... 
            //  3) if NOT a match for either of the above... 
            //          look for a GO 

            // and, in the case of ALL of the matches above ... 
            //      if <match> is in /* comments */ or in a 'string' then... it's actually NOT a match. 

            //foreach (var codeFile in this.CodeFiles)
            //{
            //    string fileBody = codeFile.FileBody;

            //    // 1. look for CREATE OR ALTER statements
            //    var regex = new Regex(@"\s*CREATE\s+OR\s+ALTER\s+(?<object>(FUNCTION|PROC|PROCEDURE|TRIGGER|VIEW))\s+", Global.StandardRegexOptions);
            //    var matches = regex.Matches(fileBody);
            //    foreach (Match match in matches)
            //    {
            //        string tSqlObjectType = match.Groups["object"].Value.ToUpperInvariant();
            //        int index = match.Groups["object"].Index;
            //        string ddlString = 


            //        Position startPosition = FileProcessor.GetFilePositionByCharacterIndex(fileBody, index);

            //        var startLine = this.Lines[startPosition.LineNumber - 1];
            //        if (startLine.StringIsCommentOrString(ddlString))
            //        {
            //            // then... the CREATE OR ALTER xxx is ... in a -- comment, /* comment */, or 'string' and doesn't count/match.
            //        }

            //    }


            // 2.A. simple - 1-part object-names
            // @"\s*(CREATE|ALTER)\s+(?<object>(AGGREGATE|ASSEMBLY|CERTIFICATE|CONTRACT|CREDENTIAL|DEFAULT|ENDPOINT|INDEX|LOGIN|QUEUE|ROLE|ROUTE|RULE|SEQUENCE|SERVICE|STATISTICS|SYNONYM|TABLE|TYPE|USER))"

            // 2.B complex objects ... 
            // @"\s*(CREATE|ALTER)\s+(?<object>\w+)" ... where object is the FIRST part of the object name... 
            //      which I then need to further parse apart. 

            // 3. GO statements (which have MUCH different logic - they 'mark' an object as the line# of the GO 'back up to' the previous/next ALTER/CREATE. 

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
                this.Errors.Add(new ParserError($"Include File: [{translatedPath}] not found.", directive.Location, "context for where the file include directive was found and stuff... "));;
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
        public string GetFileContent(string filePath);
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

        public string GetFileContent(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }
}