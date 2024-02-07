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

    public class SomethingDirective : BaseDirectiveInstance
    {
        public SomethingDirective(string name, string rawData, Location location) : base(name, rawData, location) {}
    }
}