namespace tsmake.models.directives
{
    public interface IDirectiveInstance
    {
        string Name { get; }
        string RawData { get; }
        Location Location { get; }
    }

    public abstract class BaseDirectiveInstance : IDirectiveInstance
    {
        public string Name { get; }
        public string RawData { get; }
        public Location Location { get; }

        protected BaseDirectiveInstance(string name, string rawData, Location location)
        {
            this.Name = name;
            this.RawData = rawData;
            this.Location = location;
        }
    }

    //public class SomethingDirective : BaseDirectiveInstance
    //{
    //    public SomethingDirective(string name, string rawData, Location location) : base(name, rawData, location) {}
    //}

    public class RootPathDirective : BaseDirectiveInstance
    {
        public RootPathDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class OutputDirective : BaseDirectiveInstance
    {
        public OutputDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class CommentDirective : BaseDirectiveInstance
    {
        public CommentDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class VersionCheckerDirective : BaseDirectiveInstance
    {
        public VersionCheckerDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class IncludeFileDirective : BaseDirectiveInstance
    {
        public IncludeFileDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class IncludeDirectoryDirective : BaseDirectiveInstance
    {
        public IncludeDirectoryDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }

    public class ConditionalBlockDirective : BaseDirectiveInstance
    {
        // not sure if this'll be a single directive, or a 'family' of 4x directives... 

        public ConditionalBlockDirective(string name, string rawData, Location location) : base(name, rawData, location) { }
    }
}