// NOTE: Some of the directives below are NOT needed from within VS, but ARE 100% needed by PowerShell:
global using System;
global using System.IO;
global using System.Text;
global using System.Linq;
global using System.Data.Common;
global using System.Data.Odbc;
global using System.Collections.Generic;
global using System.Text.RegularExpressions;
global using System.Management.Automation;

namespace tsmake;

public static class Global
{
    public static RegexOptions SingleLineRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
}