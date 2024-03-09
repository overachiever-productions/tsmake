using tsmake.models;

namespace tsmake
{
    public interface IError
    {
        public string ErrorMessage { get; }
        public string Context { get; }

        public string GetErrorText();
    }

    public abstract class BaseError : IError
    {
        public string ErrorMessage { get; }
        public string Context { get; protected set; }

        protected BaseError(string errorMessage, string context = "")
        {
            this.ErrorMessage = errorMessage;
            this.Context = context;
        }

        public abstract string GetErrorText();
    }

    public class ParserError : BaseError, IError
    {
        public Location Location { get;}

        public ParserError(string errorMessage, Location location, string context = "")
            : base(errorMessage, context) 
        {
            this.Location = location;

            if (string.IsNullOrEmpty(base.Context))
                base.Context = this.Location.GetLocationContext();
        }

        public override string GetErrorText()
        {
            return $"PARSER ERROR: {base.ErrorMessage} At {base.Context}";
        }
    }

    public class BuildError : BaseError, IError
    {
        public ErrorRecord ErrorRecord { get; }

        public BuildError(string errorMessage, ErrorRecord record = null, string context = "")
            : base(errorMessage, context)
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
            return $"BUILD ERROR: {base.ErrorMessage}";
        }
    }

    // TODO: 
    // public class ExecutionError (or maybe MigrationError?)  for Psi Execution of tsmake 'runner'. 
    //      could also call these RunnerErrors or RuntimeErrors. 

    // TODO: 
    // public class <whateverI'mGoingToCallBuildFileGeneration>Errors 
    //      maybe GeneratorErrors or ... whatever. 
}