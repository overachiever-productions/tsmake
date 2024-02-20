using System;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace tsmake
{
    public class Formatter
    {
        private Formatter() { }

        public bool HostSupportsColor { get; set; } // exposing this as a PUBLIC prop - not sure I'm going to use it much from 'outside' this class though.
        public static Formatter Instance => new Formatter();

        public void SetCurrentHostInfo(string name)
        {
            if (name.ToLowerInvariant() == "consolehost")
                this.HostSupportsColor = true;
            else
            {
                var regex = new Regex("console|code|remotehost");
                if (regex.IsMatch(name))
                    this.HostSupportsColor = true;
            }
        }

        public string SizedDash(int length)
        {
            string output = new String('-', length);

            if (this.HostSupportsColor)
                //output = $"\u001b[36;1m{output}\u001b[0m";
                output = $"{PSStyle.Instance.Foreground.BrightCyan}{output}{PSStyle.Instance.Reset}";

            return output;
        }

        public string ColumnHeading(int leftPadding, string name, int length)
        {
            string padding = new String(' ', length);
            string output = $"{name}{padding}".Substring(0, length);

            if (leftPadding > 0)
                output = new String(' ', leftPadding) + output;

            if (this.HostSupportsColor)
                output = $"{PSStyle.Instance.Foreground.BrightCyan}{output}{PSStyle.Instance.Reset}";

            return output;
        }

        public string ResultOutcome(int leftPadding, IProcessingResult result, string pattern, int length)
        {
            string padding = new String(' ', length);
            string output = $"{string.Format(pattern, result.Outcome.ToString().ToUpperInvariant())}{padding}".Substring(0, length);

            if (leftPadding > 0)
                output = new String(' ', leftPadding) + output;

            if (this.HostSupportsColor)
            {
                output = $"{PSStyle.Instance.Foreground.Red}{output}{PSStyle.Instance.Reset}";
            }

            return output;
        }
    }
}

