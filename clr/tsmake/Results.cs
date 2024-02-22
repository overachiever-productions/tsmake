namespace tsmake
{
    public interface IProcessingResult
    {
        // .Start and .End times? 
        public string OperationType { get; }  // might make more sense to have this as yet-another ENUM? 
        public ProcessingOutcome Outcome { get; }
        public List<ParserError> ParserErrors { get; }

        // .GetFatalErrors()
        // .GetAllErrors() ? 
    }

    public abstract class BaseProcessingResult : IProcessingResult
    {
        public string OperationType { get; protected set; }
        public ProcessingOutcome Outcome { get; private set; }
        public List<ParserError> ParserErrors { get; }

        protected BaseProcessingResult()
        {
            this.Outcome = ProcessingOutcome.Failure; // default to failed. 
            this.ParserErrors = new List<ParserError>();
        }

        public void AddParserError(ParserError error)
        {
            this.ParserErrors.Add(error);
        }

        public void SetSucceeded()
        {
            this.Outcome = ProcessingOutcome.Success;
        }
    }

    public class BuildResult : BaseProcessingResult 
    {
        // properties that this should have: 
        // .BuildFile 
        // ??? 
        public string BuildFile { get; }


        //      GUESSING that I should all all of the ? below via Set Success and/or SetFailure (i.e., even if build fails, I want to know # of lines, tokens, directives, etc)
        // .TokensCount?   - and... there are 2 potential counts: found, successfully-processed, etc. 
        // .DirectivesCount?   -- ditto on found/defined vs processed. 
        // .Lines(total)ProcessedCount ? 

        public BuildResult(string buildFile)
        {
            base.OperationType = "BUILD";  // TODO: yeah... this needs to be an enum... 
            this.BuildFile = buildFile;
        }
    }


    // then ... DocumentationResult ???


    // OTHER, eventual, results will be: 
    //      MigrationRun (or whatever I call it)
    //      git/source-controlBuild (or whatever)
    //  and so on.
}