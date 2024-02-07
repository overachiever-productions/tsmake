namespace tsmake.models
{
    public class Location
    {
        public string FileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        public Location OriginalLocation { get; }

        public Location(string fileName, int lineNumber, int columnNumber)
        {
            this.FileName = fileName;
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
        }

        public Location(string fileName, int lineNumber, int columnNumber, Location originalLocation) : this(fileName, lineNumber, columnNumber)
        {
            this.OriginalLocation = originalLocation;
        }
    }
}