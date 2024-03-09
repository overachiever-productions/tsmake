using System.Configuration;

namespace tsmake
{
    public enum OverflowType
    {
        Strip,
        Ellide,
        Wrap
    }

    public enum TextStyle
    {
        None,
        Error, 
        HeaderLine
    }

    public class FormattedLineBuffer
    {
        private int maxWidtth;
        private int leftPadding;
        private int rightPadding;
        private string bufferedContent;

        public FormattedLineBuffer(int maxWidtth, int leftPadding, int rightPadding, string bufferedContent = "")
        {
            this.maxWidtth = maxWidtth;
            this.leftPadding = leftPadding;
            this.rightPadding = rightPadding;
            this.bufferedContent = bufferedContent;
        }
    }

    public class Formatter
    {
        private int WindowWidth { get; set; }
        private bool HostSupportsColor { get; set; }

        private Formatter() { }
        
        public static Formatter Instance => new Formatter();

        public void SetCurrentHostInfo(string name, int width)
        {
            if (name.ToLowerInvariant() == "consolehost")
                this.HostSupportsColor = true;
            else
            {
                var regex = new Regex("console|code|remotehost");
                if (regex.IsMatch(name))
                    this.HostSupportsColor = true;
            }

            this.WindowWidth = width;
        }

        public string DashyDash(int leftPadding, float percent)
        {
            return new string('-', 80);
        }
        public string Dash(int leftPaddingChars, TextStyle style, float widthPercentage, int rightPaddingChars)
        {
            if (widthPercentage > 100 | widthPercentage < 10)
                throw new Exception("Invalid Percentage Specified - allowed values are 10% - 100%");

            int widthInChars = this.GetWidthInChars(widthPercentage);

            return this.Dash(leftPaddingChars, style, widthInChars, rightPaddingChars);
        }

        public string Dash(int leftPadding, TextStyle style, int width, int rightPadding)
        {
            string output = new string('-', width - leftPadding - rightPadding);
            return this.WriteLine(leftPadding, width, output, style, rightPadding, OverflowType.Strip);
        }

        public string WrappingLine(int leftPadding, string data, TextStyle style, float widthPercentage, int rightPadding)
        {
            return this.WriteLine(leftPadding, this.GetWidthInChars(widthPercentage), data, style, rightPadding, OverflowType.Wrap);
        }

        //public string SizedDash(int length)
        //{
        //    string output = new String('-', length);

        //    if (this.HostSupportsColor)
        //        //output = $"\u001b[36;1m{output}\u001b[0m";
        //        output = $"{PSStyle.Instance.Foreground.BrightCyan}{output}{PSStyle.Instance.Reset}";

        //    return output;
        //}

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

        public string ErrorSummary(int leftPadding, float widthPercentage, IError error, int rightPadding)
        {
            // TODO: replace the stuff below with error.GetErrorSummary();
            //      and .. make sure to preface the text/output with "- " ... i.e., poor man's bullets. 
            //      and then ... send it into this.writeline with the color red and ... options for elide vs wrap... and such. 
            string summary = $"- {error.GetErrorText()}";

            return this.WriteLine(leftPadding, this.GetWidthInChars(widthPercentage), summary, TextStyle.Error, 5, OverflowType.Wrap);
        }

        private string WriteLine(int leftPadding, int maxWidth, string data, TextStyle style, int rightPadding = 0, OverflowType overflow = OverflowType.Strip)
        {
            data = data.Replace(Environment.NewLine, " ", StringComparison.InvariantCultureIgnoreCase);

            string left = new string(' ', leftPadding);
            string right = new string(' ', rightPadding);
            string output = $"{left}{data}{right}";

            if (output.Length > (maxWidth - rightPadding))
            {
                switch (overflow)
                {
                    case OverflowType.Strip:
                        output = output.Substring(0, (maxWidth - rightPadding));
                        break;
                    case OverflowType.Ellide:
                        output = output.Substring(0, (maxWidth - rightPadding - 1) + '…');
                        break;
                    case OverflowType.Wrap:
                        output = this.WrapText(leftPadding, maxWidth, data, rightPadding);
                        break;
                }
            }

            if (style != TextStyle.None & this.HostSupportsColor)
            {
                string directive = this.TranslateStyleToPsStyle(style);

                output = $"{directive}{output}{PSStyle.Instance.Reset}";
            }

            return output;
        }

        // NOTE: Interestingly enough, if I did, say, 3x of these 'side by side' ... i'd have columns. 
        private string WrapText(int leftPadding, int maxWidth, string data, int rightPadding)
        {
            int totalLength = data.Length;

            if(totalLength < (maxWidth - leftPadding - rightPadding))
                return data;

            string left = "";
            string right = "";
            string indent = new string(' ', 2);

            if (leftPadding > 0)
                left = new string(' ', leftPadding);

            if (rightPadding > 0)
                right = new string(' ', rightPadding);

            bool firstLine = true;
            int currentPosition = 0;
            int grabSize;
            string currentLeft;

            StringBuilder builder = new StringBuilder();
            while (currentPosition < totalLength)
            {
                if (firstLine)
                {
                    grabSize = maxWidth - leftPadding - rightPadding;
                    currentLeft = left;
                }
                else
                {
                    grabSize = maxWidth - leftPadding - rightPadding - 2; // 2 is for the extra left-padding (indent).
                    currentLeft = left + indent;
                    builder.Append($"{Environment.NewLine}");
                }

                // logic for the LAST line (i.e., 'remainder' of the string):
                if (currentPosition + grabSize > totalLength)
                    grabSize = totalLength - currentPosition;

                string currentChunk = data.Substring(currentPosition, grabSize);
                
                builder.Append($"{currentLeft}{currentChunk}{right}");
                
                firstLine = false;
                currentPosition = currentPosition + grabSize;
            }

            return builder.ToString();
        }

        private string TranslateStyleToPsStyle(TextStyle style)
        {
            switch (style)
            {
                case TextStyle.HeaderLine:
                    return PSStyle.Instance.Foreground.BrightCyan;
                case TextStyle.Error:
                    return PSStyle.Instance.Foreground.Red;
                default:
                    throw new Exception($"tsmake Framework Error. Non-configured style: {style}.");
            }

            
        }

        private int GetWidthInChars(float targetPercentage)
        {
            // calculation in baby-steps cuz I suck at math and this makes for easier debugging:
            float actualPercentage = targetPercentage / 100.0f;
            var percentageInChars = actualPercentage * (float)this.WindowWidth;
            return (int)percentageInChars;
        }
    }
}

