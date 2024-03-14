using tsmake.models;

// TODO: 
//  Need a FULL refactor of GetErrorText(). 
//      it's currently an organic mess .... with no real rhyme or reason. 
//      Instead it should be as follows:
//      - For ParserErrors: 
//              I want/need the full location of the problem - i.e., if it's in the buildFile, i want filename, line-number, column-number. 
//                      but if it's in an INCLUDED file, I want ... build-file, line# => include file line-no (=> on down). 
//      - For BuilErrors: 
//              I want the exception/stack trace - or something similar. 
//      AND
//          for both of the above (as well as for ExecutionErrors and BuildFileGenerationErrors) I need 2x things: 
//              a. STRING representations of error info. 
//              b. some sort of struct/object that people can interrogate to get more context - i.e., for an ERRORRecord (BuildException) that's easy. 
//                          but for something like a parse-error ... i need a 'struct'/something that has the 'lineage' and other details. 


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

            // TODO: figure out what to do here... 
            //if (string.IsNullOrEmpty(base.Context))
            //    base.Context = this.Location.GetLocationContext();
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