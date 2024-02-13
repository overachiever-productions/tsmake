using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace tsmake.models.directives
{
    public interface IDirectiveInstance
    {
        string DirectiveName { get; }
        Line Line { get; }
        Location Location { get; }

        bool IsValid { get; }
        public string ValidationMessage { get; }
    }

    public abstract class BaseDirectiveInstance : IDirectiveInstance
    {
        public string DirectiveName { get; protected set; }
        public Line Line { get; }
        public Location Location { get; }
        public bool IsValid { get; protected set; }
        public string ValidationMessage { get; protected set; }

        protected BaseDirectiveInstance(Line line, Location location)
        {
            this.DirectiveName = "BASE";
            this.Line = line;
            this.Location = location;

            this.IsValid = false;  // sub-classes have to EXPLICITLY set .IsValid to true.
        }
    }

    public class RootPathDirective : BaseDirectiveInstance
    {
        public string Path { get; }
        public PathType PathType { get; }

        public RootPathDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "ROOT";

            // Two primary options for RootPath: a) hard-coded/absolute path, or b) a relative path (from xxx.build.sql file).
            // TODO: arguably.... sigh, \\unc-paths\could-be\legit\as-well\
            if(Regex.Match(line.Content, @"[A-Za-z]:\\") != null)
            {
                this.PathType = PathType.Absolute;
                base.IsValid = true;
                return;
            }

            if(Regex.Match(line.Content, @"(\.\.\\)+|[A-Za-z\.]+") != null)
            {
                this.PathType = PathType.Absolute;
                base.IsValid = true;
                return;
            }

            base.ValidationMessage = $"Invalid or missing path-directive for Directive [ROOT] in file: {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class OutputDirective : BaseDirectiveInstance
    {
        public OutputDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "OUTPUT";
        }
    }

    public class CommentDirective : BaseDirectiveInstance
    {
        public CommentDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "::";
            // And... done. 
            // As in, there's NOTHING to do here.
        }
    }

    public class VersionCheckerDirective : BaseDirectiveInstance
    {
        public VersionCheckerDirective(string name, Line line, Location location) : base(line, location) { }
    }

    public class IncludeFileDirective : BaseDirectiveInstance
    {
        public IncludeFileDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "FILE";
        }
    }

    public class IncludeDirectoryDirective : BaseDirectiveInstance
    {
        public string Path { get; }
        public PathType PathType { get; }
        public OrderBy OrderBy { get; }
        public Direction Direction { get; }
        public List<string> Exclusions { get; }
        public List<string> Priorities { get; }
        public List<string> UnPriorities { get; }

        public IncludeDirectoryDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "DIRECTORY";
            this.Exclusions = new List<string>();
            this.Priorities = new List<string>();
            this.UnPriorities = new List<string>();

            string data = line.Content.Substring(location.ColumnNumber + base.DirectiveName.Length + 1); // +1 is for the :

            Dictionary<string, string> components = new Dictionary<string, string>();

            var parts = Regex.Matches(data, @"(PATH|ORDERBY|EXCLUDE|PRIORITIES):", Global.RegexOptions).Cast<Match>().Select(m => (m.Value, m.Index)).ToArray();
            int count = parts.Length;
            if (count > 0)
            {
                string key;
                string segment;

                for (int i = 0; i < count; i++)
                {
                    var part = parts[i];
                    int start = part.Index;
                    int end = data.Length;
                    if (i < count - 1) 
                        end = parts[i + 1].Index;

                    key = part.Value.Replace(":", "");
                    segment = data.Substring(start, end - start).Replace(key, "").Trim();

                   components.Add(key, segment);
                }

                if (!components.ContainsKey("PATH"))
                {
                    segment = data.Substring(0, parts[0].Index);
                    key = "PATH";

                    components.Add(key, segment);
                }
            }
            else
            {
                // either ... the directive is equivalent to something like -- ## DIRECTORY: <path_here_and_nothing_else> 
                //      or, it's invalid... 
                string directoryPath = data.Trim();
                
                // TODO: now ... check to see if [data] could be a path/directory (without, necessarily) checking it... 
                //  and... if it could be (i.e., I need a regex for any/all valid paths - sigh)... then: 
                //      add the path and get the path-type (well, set the .Path and .PathType). 
                // OTHERWISE, drop through the bottom of this and ... .IsValid = false + .ValidationMessage = "bad syntax/directive";
                if (directoryPath.IsValidPath())
                {
                    components.Add("PATH", directoryPath);
                }
                else
                {
                    this.IsValid = false;
                    this.ValidationMessage =
                        "Something about incorrect syntax. Needed/expected ORDERBY, EXCLUDE, PRIORITIES and (optionally) PATH... but not found";
                }
            }

            // TODO: ensure that I don't need to check this ... think i probably do... 
            this.Path = components["PATH"];
            this.PathType = this.Path.GetPathType();

            if (components.ContainsKey("ORDERBY"))
            {
                data = components["ORDERBY"];

                // NOTE: not even 'bothering' to check for entire 'words' here - just enough to know that the intention is/was specified (or not).
                if (data.Contains("modi"))
                {
                    this.OrderBy = OrderBy.ModifyDate;
                }
                else
                {
                    if (data.Contains("crea")) 
                    {
                        this.OrderBy = OrderBy.CreateDate;
                    }
                    else
                        this.OrderBy = OrderBy.Alphabetical; // note that this ends up being the default (i.e., if nothing is explicitly specified).
                }

                if (data.Contains("desc"))
                    this.Direction = Direction.Descending;
                else 
                    this.Direction = Direction.Ascending;  // also the default - if nothing is explicitly specified.
            }

            if (components.ContainsKey("EXCLUDE"))
            {
                this.Exclusions.AddRange(components["EXCLUDE"].Split(",")); 
            }

            if (components.ContainsKey("PRIORITIES"))
            {
                string[] priorities = components["PRIORITIES"].Split(";");

                this.Priorities.AddRange(priorities[0].Split(","));

                // TODO: there's arguably a bug/problem here if .Length > 1 (i.e., that'd mean 2x (or more) semi-colons - which should be a syntax error. 
                //      i.e., only valid options should be length of 0 (nothing was entered), 1 (only priorities were set), or 2 (priorities and unpriorities were specified).
                if (priorities.Length > 1)
                    this.UnPriorities.AddRange(priorities[1].Split(","));
            }

            this.IsValid = true;
        }
    }

    public class ConditionalBlockDirective : BaseDirectiveInstance
    {
        // not sure if this'll be a single directive, or a 'family' of 4x directives... 

        public ConditionalBlockDirective(Line line, Location location) : base(line, location) { }
    }
}