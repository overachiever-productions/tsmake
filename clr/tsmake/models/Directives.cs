namespace tsmake.models
{
    public interface IDirective
    {
        string DirectiveName { get; }
        Line Line { get; }
        Location Location { get; }

        bool IsValid { get; }
        public string ValidationMessage { get; }
    }

    public abstract class BaseDirective : IDirective
    {
        public string DirectiveName { get; protected set; }
        public Line Line { get; }
        public Location Location { get; }
        public bool IsValid { get; protected set; }
        public string ValidationMessage { get; protected set; }

        protected BaseDirective(Line line, Location location)
        {
            DirectiveName = "BASE";
            Line = line;
            Location = location;

            IsValid = false;  // sub-classes have to EXPLICITLY set .IsValid to true.
        }
    }

    public class RootPathDirective : BaseDirective
    {
        public string Path { get; }
        public PathType PathType { get; }

        public RootPathDirective(Line line, Location location) : base(line, location)
        {
            DirectiveName = "ROOT";

            string data = this.GetDirectiveLineData();

            // Two primary options for RootPath: a) hard-coded/absolute path (e.g., local windows path like F:\blah, OR a UNC file-share path), or b) a relative path (from xxx.build.sql file).
            if (data.IsValidPath())
            {
                // TODO: don't allow PathType.Rooted for ... the Root. Those can ONLY be used once this is set.
                PathType = data.GetPathType();
                Path = data;

                IsValid = true;
            }
            else
                ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [ROOT] in file: {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class OutputDirective : BaseDirective
    {
        public string Path { get; }
        public PathType PathType { get; }

        public OutputDirective(Line line, Location location) : base(line, location)
        {
            DirectiveName = "OUTPUT";

            string data = this.GetDirectiveLineData();

            // Two primary options for RootPath: a) hard-coded/absolute path (e.g., local windows path like F:\blah, OR a UNC file-share path), or b) a relative path (from xxx.build.sql file).
            if (data.IsValidPath())
            {
                PathType = data.GetPathType();
                Path = data;

                IsValid = true;
            }
            else
                ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [OUTPUT] in file: {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class CommentDirective : BaseDirective
    {
        public CommentDirective(Line line, Location location) : base(line, location)
        {
            DirectiveName = "COMMENT";
            // And... done. 
            // As in, there's NOTHING to do here.
        }
    }

    public class VersionCheckerDirective : BaseDirective
    {
        public VersionCheckerDirective(string name, Line line, Location location) : base(line, location) { }
    }

    // REFACTOR: IncludeFileDirective, RootPathDirective, and OutputDirective all have - for all intents and purposes - the SAME underlying functionality IN THE .CTOR
    //      in essence, they all: a) get the directive data/input, b) check to see if it's a valid path and assign it + type if it is, c) throw an exception if not valid path. 
    //      ultimately, in terms of .ctor logic - the ONLY thing that's different is the error message. 
    public class IncludeFileDirective : BaseDirective
    {
        public string Path { get; }
        public PathType PathType { get; }

        public IncludeFileDirective(Line line, Location location) : base(line, location)
        {
            DirectiveName = "FILE";

            string data = this.GetDirectiveLineData();
            if (data.IsValidPath())
            {
                PathType = data.GetPathType();
                Path = data;

                IsValid = true;
            }
            else
                ValidationMessage = $"Invalid (or missing) File-Path Data for Directive [INCLUDEFILE] in file {location.FileName}, line: {location.LineNumber}.";
        }
    }

    public class IncludeDirectoryDirective : BaseDirective
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
            DirectiveName = "DIRECTORY";
            Exclusions = new List<string>();
            Priorities = new List<string>();
            UnPriorities = new List<string>();

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
                    IsValid = false;
                    ValidationMessage =
                        "Something about incorrect syntax. Needed/expected ORDERBY, EXCLUDE, PRIORITIES and (optionally) PATH... but not found";
                }
            }

            // TODO: ensure that I don't need to (error-check that this isn't null) check this ... think i probably do... 
            Path = components["PATH"];
            PathType = Path.GetPathType();

            if (components.ContainsKey("ORDERBY"))
            {
                data = components["ORDERBY"];

                // NOTE: not even 'bothering' to check for entire 'words' here - just enough to know that the intention is/was specified (or not).
                if (data.ToLowerInvariant().Contains("modi"))
                {
                    OrderBy = OrderBy.ModifyDate;
                }
                else
                {
                    if (data.ToLowerInvariant().Contains("crea"))
                    {
                        OrderBy = OrderBy.CreateDate;
                    }
                    else
                        OrderBy = OrderBy.Alphabetical; // note that this ends up being the default (i.e., if nothing is explicitly specified).
                }

                if (data.ToLowerInvariant().Contains("desc"))
                    Direction = Direction.Descending;
                else
                    Direction = Direction.Ascending;  // also the default - if nothing is explicitly specified.
            }

            if (components.ContainsKey("EXCLUDE"))
            {
                Exclusions.AddRange(components["EXCLUDE"].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }

            if (components.ContainsKey("PRIORITIES"))
            {
                string[] priorities = components["PRIORITIES"].Split(";", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

                switch (priorities.Length)
                {
                    case 0:
                        break;
                    case 1:
                        Priorities.AddRange(priorities[0].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    case 2:
                        Priorities.AddRange(priorities[0].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        UnPriorities.AddRange(priorities[1].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    default:
                        ValidationMessage = "Invalid Syntax for PRIORITIES: - multiple semi-colons (;) found - should only be 1.";
                        return;
                }
            }

            IsValid = true;
        }
    }

    public class ConditionalBlockDirective : BaseDirective
    {
        // not sure if this'll be a single directive, or a 'family' of 4x directives... 

        public ConditionalBlockDirective(Line line, Location location) : base(line, location) { }
    }

    public class DirectiveFactory
    {
        public static IDirective CreateDirective(string directiveName, Line line, Location location)
        {
            switch (directiveName)
            {
                case "::": // or COMMENT... 
                    return new CommentDirective(line, location);
                case "ROOT":
                    return new RootPathDirective(line, location);
                case "OUTPUT":
                    return new OutputDirective(line, location);
                case "FILE":
                    return new IncludeFileDirective(line, location);
                case "DIRECTORY":
                    return new IncludeDirectoryDirective(line, location);
                default:
                    throw new InvalidCastException($"Unknown Directive: {directiveName}.");
            }
        }
    }
}