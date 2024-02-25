using tsmake.models;

namespace tsmake
{
    // REFACTOR: figure out if/how-to collapse Parser, Runtime, and other errors down to an interface (or set of interfaces) + base classes. 
    //      that said, RuntimeErrors are different from ParserErrors in that ParserErrors _REQUIRE_ a Location/Context - whereas runtime errors do not. 
    //      in fact, RuntimeErrors are, effectively, 'wrappers' for Exceptions. They CAN have context and 'guide' users (when I can clearly see what's going on)
    //          ... but they might also be the result of global-ish try/catch problems and goofy scenarios I've never thought of. 

    public interface IError
    {
        public ErrorSeverity Severity { get; }
        public string ErrorMessage { get; }
        public string Context { get; }

        public string GetErrorText();
    }

    public abstract class BaseError : IError
    {
        public ErrorSeverity Severity { get; }
        public string ErrorMessage { get; }
        public string Context { get; protected set; }

        protected BaseError(ErrorSeverity severity, string errorMessage, string context = "")
        {
            this.Severity = severity;
            this.ErrorMessage = errorMessage;
            this.Context = context;
        }

        public abstract string GetErrorText();
    }

    public class ParserError : BaseError, IError
    {
        public Location Location { get;}

        public ParserError(ErrorSeverity severity, string errorMessage, Location location, string context = "")
            : base(severity, errorMessage, context) 
        {
            this.Location = location;

            if (string.IsNullOrEmpty(base.Context))
                base.Context = this.Location.GetLocationContext();
        }

        public override string GetErrorText()
        {
            return $"PARSER ERROR: {base.ErrorMessage} at {base.Context}";
        }
    }

    public class BuildError : BaseError, IError
    {
        public ErrorRecord ErrorRecord { get; }

        public BuildError(ErrorSeverity severity, string errorMessage, ErrorRecord record = null, string context = "")
            : base(severity, errorMessage, context)
        {
            this.ErrorRecord = record;

            if (string.IsNullOrEmpty(base.Context))
            {
                if (this.ErrorRecord != null)
                {
                    base.Context = record.ScriptStackTrace;
                }
            }
        }

        public override string GetErrorText()
        {
            throw new NotImplementedException();
        }
    }

    // TODO: 
    // public class ExecutionError (or maybe MigrationError?)  for Psi Execution of tsmake 'runner'. 
    //      could also call these RunnerErrors or RuntimeErrors. 

    // TODO: 
    // public class <whateverI'mGoingToCallBuildFileGeneration>Errors 
    //      maybe GeneratorErrors or ... whatever. 
}