// NOTE: Some of the directives below are NOT needed from within VS, but ARE needed by PowerShell:
// NOTE: Current Project Targets Frameworks: net8.0 (but PowerShell can/will compile to whatever version it wants/needs to use).  
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
}