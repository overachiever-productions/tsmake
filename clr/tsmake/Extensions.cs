using System.Text.RegularExpressions;

namespace tsmake
{
    public static class ExtensionMethods
    {
        public static PathType GetPathType(this string input)
        {
            if (input.StartsWith(@"///"))
                return PathType.Rooted;
            
            // TODO: need to account for the weird case of someone using a UNC share for a build... e.g., \\server-name\folder\something-to-add-to-build.whatever.
            if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.RegexOptions))
                return PathType.Rooted;

            return PathType.Relative;
        }

        public static bool IsValidPath(this string input)
        {
            // NOTE:  This func attempts to white-list known valid patterns - anything else is going to drop-out as FALSE.

            // TODO: a number of these rules are really going to depend upon which Environment.OSVersion.Platform we're on... 
            // right now, these are ONLY coded for Windows. (i.e., code below is naive and ONLY expects windows paths/validation-rules).

            if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.RegexOptions))
            {
                // now make sure that there aren't any invalid characters:
                if (Regex.IsMatch(input, @"(\<|\>|""|\||\?|\*)+", Global.RegexOptions))
                {
                    return false;
                }

                // ARGUABLY, could/should look for additional problems like: NULL byte, ASCII 0 - 31, reserved filenames (windows), and other rules
                //  as per: https://stackoverflow.com/a/31976060/11191 

                return true;
            }

            return false;
        }
    }
}