using tsmake.models;

namespace tsmake
{
    // REFACTOR: figure out if/how-to collapse Parser, Runtime, and other errors down to an interface (or set of interfaces) + base classes. 
    //      that said, RuntimeErrors are different from ParserErrors in that ParserErrors _REQUIRE_ a Location/Context - whereas runtime errors do not. 
    //      in fact, RuntimeErrors are, effectively, 'wrappers' for Exceptions. They CAN have context and 'guide' users (when I can clearly see what's going on)
    //          ... but they might also be the result of global-ish try/catch problems and goofy scenarios I've never thought of. 


    public class ParserError
    {
        public ErrorSeverity Severity { get; }
        // might? want to put a priority into place? so that if/when there are multiples... they get sorted by Severity, Priority and output in that regard?
        public Location Location { get; }
        public string ErrorMessage { get; }
        public string Context { get; private set; }

        public ParserError(ErrorSeverity severity, Location location, string errorMessage, string context = "")
        {
            this.Severity = severity;
            this.Location = location;
            this.ErrorMessage = errorMessage;

            if (context != "")
                this.Context = context;
            else
                this.SetContext();
        }

        private void SetContext()
        {
            this.Context =
                $"In file: [{this.Location.FileName}], on Line #: {this.Location.LineNumber} - Column: {this.Location.ColumnNumber}.";
        }
    }

    // REFACTOR: might make more sense to call this a BuildError - because it happens during build. 
    //      then, I can call errors during Psi Execution of a 'migration' script either MigrationErrors or ExecutionErrors. 
    //      i.e., I defaulted to using an abstract/generic name (Runtime) here ... without thinking of the OTHER kinds of errors that might exist.
    public class RuntimeError
    {
        public ErrorSeverity Severity { get; }
        public Location Location { get; }
        public ErrorRecord ErrorRecord { get; }
        public string ErrorMessage { get; }
        public string Context { get; }

        public RuntimeError(ErrorSeverity severity, string errorMessage, ErrorRecord errorRecord = null, string context = "", Location location = null)
        {
            this.Severity = severity;
            this.ErrorMessage = errorMessage;

            this.ErrorRecord = errorRecord;
            this.Context = context;
            this.Location = location;
        }
    }
}