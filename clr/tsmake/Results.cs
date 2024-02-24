namespace tsmake
{
    public interface IProcessingResult
    {
        public bool HasFatalError { get; }
        public OperationType OperationType { get; } 
        public ProcessingOutcome Outcome { get; }
        public List<IError> FatalErrors { get; }
        public List<IError> NonFatalErrors { get; }
        // .Start and .End times? 
    }

    public abstract class BaseProcessingResult : IProcessingResult
    {
        public bool HasFatalError => this.FatalErrors.Count > 0;
        public OperationType OperationType { get; protected set; }
        public ProcessingOutcome Outcome { get; private set; }
        public List<IError> FatalErrors { get; }
        public List<IError> NonFatalErrors { get; }

        protected BaseProcessingResult()
        {
            this.Outcome = ProcessingOutcome.Failure; // default to failed. 
            this.FatalErrors = new List<IError>();
            this.NonFatalErrors = new List<IError>();
        }

        public void AddFatalError(IError error)
        {
            this.FatalErrors.Add(error);
        }

        public void AddNonFatalError(IError error)
        {
            this.NonFatalErrors.Add(error);
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