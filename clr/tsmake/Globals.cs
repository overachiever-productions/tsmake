using System.Text.RegularExpressions;

// TODO: see if I can add GLOBAL USINGs here... i.e., can I do that here and have it work in a) VS, and b) PowerShell Studio? and ... c) normal builds? 


namespace tsmake
{
    public static class Global
    {
        public static RegexOptions RegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    }
}