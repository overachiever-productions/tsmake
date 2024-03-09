namespace tsmake
{
    public interface IProcessingResult
    {
        public bool HasErrors { get; }
        public OperationType OperationType { get; } 
        public ProcessingOutcome Outcome { get; }
        public List<IError> Errors { get; }
        // .Start and .End times? 
    }

    public abstract class BaseProcessingResult : IProcessingResult
    {
        public bool HasErrors => this.Errors.Count > 0;
        public OperationType OperationType { get; protected set; }
        public ProcessingOutcome Outcome { get; private set; }
        public List<IError> Errors { get; }

        protected BaseProcessingResult()
        {
            this.Outcome = ProcessingOutcome.Failure; // default to failed. 
            this.Errors = new List<IError>();
        }

        public void AddError(IError error)
        {
            this.Errors.Add(error);
        }

        public void AddErrors(List<IError> errors)
        {
            this.Errors.AddRange(errors);
        }

        public void SetSucceeded()
        {
            this.Outcome = ProcessingOutcome.Success;
        }
    }

    public class BuildResult : BaseProcessingResult 
    {
        public string BuildFile { get; }

        //      GUESSING that I should all all of the ? below via Set Success and/or SetFailure (i.e., even if build fails, I want to know # of lines, tokens, directives, etc)
        // .TokensCount?   - and... there are 2 potential counts: found, successfully-processed, etc. 
        // .DirectivesCount?   -- ditto on found/defined vs processed. 
        // .Lines(total)ProcessedCount ? 

        public BuildResult(string buildFile, string verb)
        {
            this.BuildFile = buildFile;

            if (verb.ToLowerInvariant() == "both")
                base.OperationType = OperationType.DocsAndBuild;
            else
                base.OperationType = OperationType.Build;
        }
    }


    // then ... DocumentationResult ???


    // OTHER, eventual, results will be: 
    //      MigrationRun (or whatever I call it)
    //      git/source-controlBuild (or whatever)  (generator?)
    //  and so on.
}