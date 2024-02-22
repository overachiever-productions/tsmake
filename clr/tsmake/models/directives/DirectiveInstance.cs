namespace tsmake.models.directives
{
    // REFACTOR: I'm getting old and stupid. Tokens can/will have TokenDefinitions and TokenInstances - cuz end-users are allowed
    //      to define tokens. End-users can't define Directives... so, i don't really need to differentiate between Directives and Directive 'Instances'. 
    //      in short, these can simply become Directives.
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

            string data = this.GetDirectiveLineData();

            // Two primary options for RootPath: a) hard-coded/absolute path (e.g., local windows path like F:\blah, OR a UNC file-share path), or b) a relative path (from xxx.build.sql file).
            if (data.IsValidPath())
            {
                // TODO: don't allow PathType.Rooted for ... the Root. Those can ONLY be used once this is set.
                this.PathType = data.GetPathType();
                this.Path = data;

                this.IsValid = true;
            }
            else 
                base.ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [ROOT] in file: {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class OutputDirective : BaseDirectiveInstance
    {
        public string Path { get; }
        public PathType PathType { get; }

        public OutputDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "OUTPUT";

            string data = this.GetDirectiveLineData();

            // Two primary options for RootPath: a) hard-coded/absolute path (e.g., local windows path like F:\blah, OR a UNC file-share path), or b) a relative path (from xxx.build.sql file).
            if (data.IsValidPath())
            {
                this.PathType = data.GetPathType();
                this.Path = data;

                this.IsValid = true;
            }
            else 
                base.ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [OUTPUT] in file: {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class CommentDirective : BaseDirectiveInstance
    {
        public CommentDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "COMMENT";
            // And... done. 
            // As in, there's NOTHING to do here.
        }
    }

    public class VersionCheckerDirective : BaseDirectiveInstance
    {
        public VersionCheckerDirective(string name, Line line, Location location) : base(line, location) { }
    }

    // REFACTOR: IncludeFileDirective, RootPathDirective, and OutputDirective all have - for all intents and purposes - the SAME underlying functionality IN THE .CTOR
    //      in essence, they all: a) get the directive data/input, b) check to see if it's a valid path and assign it + type if it is, c) throw an exception if not valid path. 
    //      ultimately, in terms of .ctor logic - the ONLY thing that's different is the error message. 
    public class IncludeFileDirective : BaseDirectiveInstance
    {
        public string Path { get; }
        public PathType PathType { get; }

        public IncludeFileDirective(Line line, Location location) : base(line, location)
        {
            base.DirectiveName = "FILE";

            string data = this.GetDirectiveLineData();
            if (data.IsValidPath())
            {
                this.PathType = data.GetPathType();
                this.Path = data;

                this.IsValid = true;
            }
            else 
                base.ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [INCLUDEFILE] in file {location.FileName}, line: {location.LineNumber}.";
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

            string data = this.GetDirectiveLineData();

            Dictionary<string, string> components = new Dictionary<string, string>();

            var parts = Regex.Matches(data, @"(PATH|ORDERBY|EXCLUDE|PRIORITIES):", Global.RegexOptions).Cast<Match>().Select(m => (m.Value, m.Index)).ToArray();
            int count = parts.Length;
            if (count > 0)
            {
                string key;
                string segment;

                for (int i = 0; i < count; i++)
                {
                    int start = parts[i].Index;
                    int end = data.Length;
                    if (i < count - 1) 
                        end = parts[i + 1].Index;

                    key = parts[i].Value.Replace(":", "");
                    segment = data.Substring(start, end - start).Replace(key, "").Replace(":", "").Trim();

                    components.Add(key, segment);
                }

                if (!components.ContainsKey("PATH"))
                {
                    segment = data.Substring(0, parts[0].Index);
                    components.Add("PATH", segment);
                }
            }
            else
            {
                // if we didn't match on sub-directives, then either: a) this is a simple DIRECTORY directive with <path_and_nothing_else> or, b) it's invalid.
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

            // TODO: ensure that I don't need to (error-check that this isn't null) check this ... think i probably do... 
            this.Path = components["PATH"];
            this.PathType = this.Path.GetPathType();

            if (components.ContainsKey("ORDERBY"))
            {
                data = components["ORDERBY"];

                // NOTE: not even 'bothering' to check for entire 'words' here - just enough to know that the intention is/was specified (or not).
                if (data.ToLowerInvariant().Contains("modi"))
                {
                    this.OrderBy = OrderBy.ModifyDate;
                }
                else
                {
                    if (data.ToLowerInvariant().Contains("crea")) 
                    {
                        this.OrderBy = OrderBy.CreateDate;
                    }
                    else
                        this.OrderBy = OrderBy.Alphabetical; // note that this ends up being the default (i.e., if nothing is explicitly specified).
                }

                if (data.ToLowerInvariant().Contains("desc"))
                    this.Direction = Direction.Descending;
                else 
                    this.Direction = Direction.Ascending;  // also the default - if nothing is explicitly specified.
            }

            if (components.ContainsKey("EXCLUDE"))
            {
                this.Exclusions.AddRange(components["EXCLUDE"].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim())); 
            }

            if (components.ContainsKey("PRIORITIES"))
            {
                string[] priorities = components["PRIORITIES"].Split(";", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

                switch (priorities.Length)
                {
                    case 0:
                        break;
                    case 1:
                        this.Priorities.AddRange(priorities[0].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    case 2:
                        this.Priorities.AddRange(priorities[0].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        this.UnPriorities.AddRange(priorities[1].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    default:
                        this.ValidationMessage = "Invalid Syntax for PRIORITIES: - multiple semi-colons (;) found - should only be 1.";
                        return;
                }
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