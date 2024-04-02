// WARNING: Visual Studio will show MANY of these global usings as non-needed - but PowerShell's C# compiler WILL need these usings.
global using System;
global using System.IO;
global using System.Collections.Generic;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Management.Automation;


namespace tsmake
{
    public static class Global
    {
        public static RegexOptions StandardRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    }
}