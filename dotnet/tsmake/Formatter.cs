namespace tsmake;

public class Formatter
{
    private Formatter() { }

    public bool HostSupportsColor { get; set; }

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

    public string SimpleString(string content, int indent = 0)
    {
        string padding = "";
        if (indent > 0)
            padding += new String(' ', indent);

        return $"{padding}{content}";
    }

    public string ColoredString(string content, string color, int indent = 0)
    {
        string padding = "";
        if (indent > 0)
            padding += new String(' ', indent);

        var output = $"{padding}{content}";

        if (this.HostSupportsColor)
            output = $"{this.ParseColor(color)}{output}{PSStyle.Instance.Reset}";

        return output;
    }

    public string GetBuildWrapperOutcome(BuildWrapper wrapper)
    {
        // if all results pass or .HasErrors = false... then ... green. 
        // if all results fail or there are ... ugly kinds of exceptions (runtime?)
        //      then ... red. 
        // if some passed, some failed... then yellow? 

        return this.ColoredString("TBD", "yellow");
    }

    private string ParseColor(string color)
    {
        // hack for now:
        switch (color.ToLowerInvariant())
        {
            case "red":
                return PSStyle.Instance.Foreground.Red;
            case "green":
                return PSStyle.Instance.Foreground.Green;
            case "yellow":
                return PSStyle.Instance.Foreground.BrightYellow;
            case "cyan":
                return PSStyle.Instance.Foreground.BrightCyan;
            default:
                throw new Exception("ruh roh");
        }

        // i.e., need to just add some logic to PARSE an enum of the type in questi8on
    }
}