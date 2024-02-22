namespace tsmake.models.directives
{
    public class DirectiveFactory
    {
        public static IDirectiveInstance CreateDirective(string directiveName, Line line, Location location)
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