namespace tsmake.models.directives
{
    public interface IDirectiveInstance
    {
        string Name { get; }
        Line Line { get; }
        Location Location { get; }

        // bool IsValid { get; }
    }

    public abstract class BaseDirectiveInstance : IDirectiveInstance
    {
        public string Name { get; }
        public Line Line { get; }
        public Location Location { get; }

        protected BaseDirectiveInstance(string name, Line line, Location location)
        {
            this.Name = name;
            this.Line = line;
            this.Location = location;
        }
    }

    public class RootPathDirective : BaseDirectiveInstance
    {
        public RootPathDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class OutputDirective : BaseDirectiveInstance
    {
        public OutputDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class CommentDirective : BaseDirectiveInstance
    {
        public CommentDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class VersionCheckerDirective : BaseDirectiveInstance
    {
        public VersionCheckerDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class IncludeFileDirective : BaseDirectiveInstance
    {
        public IncludeFileDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class IncludeDirectoryDirective : BaseDirectiveInstance
    {
        public IncludeDirectoryDirective(string name, Line line, Location location) : base(name, line, location) { }
    }

    public class ConditionalBlockDirective : BaseDirectiveInstance
    {
        // not sure if this'll be a single directive, or a 'family' of 4x directives... 

        public ConditionalBlockDirective(string name, Line line, Location location) : base(name, line, location) { }
    }
}