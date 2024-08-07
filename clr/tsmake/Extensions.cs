﻿using tsmake.models;

namespace tsmake
{
    public static class ExtensionMethods
    {
        internal static PathType GetPathType(this string input, bool strict = false)
        {
            if (strict)
            {
                if (!input.IsValidPath())
                    throw new InvalidOperationException("tsmake Workflow Exception: Can not evaluate PathType when Path is deemed invalid.");
            }

            if (input.StartsWith(@"\\\"))
                return PathType.Rooted;
            
            // Absolute - Local File
            if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.StandardRegexOptions))
                return PathType.Absolute;

            // Absolute - UNC Share
            if (input.StartsWith("//"))
                return PathType.Absolute;

            return PathType.Relative;
        }

        public static bool IsValidPath(this string input)
        {
            // NOTE:  This func attempts to white-list known valid patterns - anything else is going to drop-out as FALSE.

            // Absolute Path - local machine.
            if (Regex.IsMatch(input, @"^[A-Za-z]{1}:\\", Global.StandardRegexOptions))
            {
                if(FileOrDirectoryExists(input))
                    return true;

                if (PathContainsIllegalCharacters(input))
                    return false;

                return true; 
            }

            // Absolute Path - but against a UNC share:
            if (input.StartsWith(@"//"))
            {
                if (FileOrDirectoryExists(input))
                    return true;

                if (PathContainsIllegalCharacters(input))
                    return false;

                return true;
            }

            // Relative Path - but from the current directory (i.e., no / in the path):
            if (!input.ToLowerInvariant().Contains("/"))
            {
                if (PathContainsIllegalCharacters(input))
                    return false;

                return true;
            }

            // Relative Path - but 'up' from current directory. 
            if (input.StartsWith(@"../"))
            {
                if (PathContainsIllegalCharacters(input))
                    return false;

                return true;
            }

            // Relative Path - but in child directory: 
            if (input.ToLowerInvariant().Contains("/"))
            {
                if (PathContainsIllegalCharacters(input))
                    return false;

                return true;
            }

            return false;
        }

        public static bool FileOrDirectoryExists(string path)
        {
            return (Directory.Exists(path) || File.Exists(path));
        }

        public static bool PathContainsIllegalCharacters(string path)
        {
            // See: https://stackoverflow.com/a/31976060/11191 

            // TODO: need to key this against current OS (i.e., Environment.Platform/etc.)
            if (Regex.IsMatch(path, @"(\<|\>|""|\||\?|\*)+", Global.StandardRegexOptions))
                return true;

            // ARGUABLY, could/should look for additional problems like: NULL byte, ASCII 0 - 31, reserved filenames (windows), and other rules

            return false;
        }

        public static string GetDirectiveLineData(this IDirective instance, bool removeTsMakeLineComments = true)
        {
            // TODO: I MAY have a bug with my logic/REGEX where: 1) I've been assuming that directives have XXXX: syntax (where the colon HAS to be immediately after the name)
            //      only... it MAY BE that ... I can have directives in the form of "XXXX      :" and so on... 
            //          i.e., check/test that. 
            string data = instance.Line.RawContent.Substring(instance.Location.Peek().Column + instance.DirectiveName.Length + 1).Trim(); // +1 is for the ":"

            if (!removeTsMakeLineComments)
                return data;

            // TODO: using ##:: as comment - for now... 
            if (data.ToLowerInvariant().Contains("##::"))
            {
                var parts = data.Split("##::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts[0];
            }

            return data;
        }

        public static string WildcardToRegex(this string pattern)
        {
            // TODO: add an option for `_ which'll be the equivalent of LIKE '`_` ESCAPE '`' ... (or, maybe?, just set up the equivalent of ESCAPE '`') ... 
            return Regex.Escape(pattern).Replace(@"%", ".*").Replace(@"_", ".{1}");
        }

        public static bool Like(this string input, string pattern)
        {
            return Regex.IsMatch(input, pattern.WildcardToRegex(), Global.StandardRegexOptions);
        }

        public static string CollapsePath(this string path, string directive)
        {
            string newPath = path;
            string newDirective = directive;

            while (newDirective.StartsWith(@"..\"))
            {
                newPath = Directory.GetParent(newPath).FullName;
                newDirective = newDirective.Substring(3);
            }

            string output = Path.Join(newPath, newDirective);
            return output;
        }

        public static Stack<T> DeepClone<T>(this Stack<T> original) where T : Location
        {
            var array = new T[original.Count];

            int i = 0;
            foreach (var item in original)
            {
                array[i] = (T)item.Clone();
                i++;
            }
            
            Array.Reverse(array);
            return new Stack<T>(array);
        }
        
        public static string GetLocation<T>(this Stack<T> locationStack, string indent = "\t", bool increaseIndent = true) where T : Location
        {
            if (locationStack.Count == 1)
                return locationStack.Peek().FileName + " (" + locationStack.Peek().LineNumber + ", " + locationStack.Peek().Column + ")";

            var sb = new StringBuilder();
            var locationClone = locationStack.DeepClone();
            var currentIndent = indent;
            if (increaseIndent) currentIndent = "";
            while (locationClone.Count > 0)
            {
                var location = locationClone.Pop();
                sb.AppendLine($"{currentIndent}{location.FileName} ({location.LineNumber}, {location.Column})");
                if (increaseIndent)
                    currentIndent += indent;
            }

            return sb.ToString();
        }
    }
}