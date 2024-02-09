using System;

namespace tsmake.models.directives
{
    public class DirectiveFactory
    {
        public static IDirectiveInstance CreateDirective(string directiveName, Line line, Location location)
        {
            switch (directiveName)
            {
                case "OUTPUT":
                    return new OutputDirective(directiveName, line, location);
                case "FILE":
                    return new IncludeFileDirective(directiveName, line, location);
                case "DIRECTORY":
                    return new IncludeDirectoryDirective(directiveName, line, location);
                case "NOTE": // or COMMENT... 
                    // TODO: need to work on this - don't think I want to require ##NOTE as the actual ... directive.... i think ##: or ##:: might just be it? 
                    //      i.e., I'm going to have to 1) figure out what I want for syntax rules/options and 2) implement - which'll probably mean some sort of 
                    //      semi-hack/work-around where I look for, say, a) normal directives, and then b) ##:: as a specialized directive and .. if I find it ... 'mark' it in some way so that it can get passed into this... 
                    return new CommentDirective(directiveName, line, location);
                default:
                    throw new InvalidCastException($"Unknown Directive: {directiveName}.");
            }


            // hack/proof-of-concept: 
            //return new SomethingDirective(directiveName, line.Content, location);
        }
    }
}