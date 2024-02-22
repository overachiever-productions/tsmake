namespace tsmake.models
{
    // REFACTOR: these can be called Tokens (yeah, they're instances, but it's clear that TokenDefinitions are the ... definitions). 
    public class TokenInstance
    {
        public string Name { get; }
        public string DefaultValue { get; }
        public Location Location { get; }

        public TokenInstance(string tokenValue, Location location)
        {
            if (tokenValue.Contains(":"))
            {
                var parts = tokenValue.Split(':');
                this.Name = parts[0].ToUpperInvariant();
                this.DefaultValue = parts[1];
            }
            else
            {
                this.Name = tokenValue.ToUpperInvariant();
            }

            this.Location = location;
        }
    }
}