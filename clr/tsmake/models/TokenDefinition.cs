namespace tsmake.models
{
    public class TokenDefinition
    {
        public string Name { get; }
        public string DefaultBuildValue { get; }
        public string SpecifiedBuildValue { get; set; }
        public bool AllowBlanks { get; }
        public bool AllowInlineDefaults { get; }

        public TokenDefinition(string name, string specifiedValue) : this(name, specifiedValue, "") { }

        public TokenDefinition(string name, string specifiedBuildValue, string defaultBuildValue, bool allowInlineDefaults = false, bool allowBlanks = false) 
        {
            this.Name = name;
            this.DefaultBuildValue = defaultBuildValue;
            this.SpecifiedBuildValue = specifiedBuildValue;
            this.AllowInlineDefaults = allowInlineDefaults;
            this.AllowBlanks = allowBlanks;
        }
    }
}