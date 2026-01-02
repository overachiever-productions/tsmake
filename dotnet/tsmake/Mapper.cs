namespace tsmake;

public class Mapper : IMapper
{
    /*
        
        I think the main flow of this will be: 
            - look for any un-closed strings or block-comments - as per (roughly) line 28-ish of this file: D:\Dropbox\Repositories\tst\dotnet\tst.Models\Tokenizers\Sanitizer.cs
            
            - assuming that works well:
                - run a regex against the entire body/string-input (passed in via the .ctor)
                - regex will match against: 
                    - strings (which I can skip/dump). ONLY using these to make sure that GO, CREATE/ALTER, and comments aren't inside strings.
                    - comments (single-line and multi-line)
                    - GO statements 
                    - CREATE/ALTER statements (for procs, functions, views, triggers)   
            - for each match, create an object (CommentBatch, GoBatch, ObjectBatch) and add to the appropriate List<x>. 
            - I don't NEED heavy-duty details on each match. 
                Just enough to know the type (so I can put it in the correct List<x>), the start/end offsets, and the text.

    */

    private readonly string _rawText;

    public List<ISyntaxError> SyntaxErrors { get; }
    public List<IBatch> Batches { get; }
    public List<IEolComment> EolComments { get; }
    public List<IBlockComment> BlockComments { get; }
    public List<IObjectDeclaration> ObjectDeclarations { get; }

    // 'normalizedText' is _expected_ to be pre-processed by a Normalizer instance (i.e., by CONVENTION only). I could 'enforce' this by means of an INormalizedString interface ... which'd have a .String and a .CrLfEndingOptions ... similar to an 'HtmlString' in MVC.
    public Mapper(string normalizedText)
    {
        this._rawText = normalizedText;

        this.SyntaxErrors = new List<ISyntaxError>();
        this.Batches = new List<IBatch>();
        this.EolComments = new List<IEolComment>();
        this.BlockComments = new List<IBlockComment>();
        this.ObjectDeclarations = new List<IObjectDeclaration>();

        this.ValidateClosures();

        if (this.SyntaxErrors.Count > 0)
            return;

        this.Map(normalizedText);
    }

    private void ValidateClosures()
    {
        // https://overachieverllc.atlassian.net/browse/TSM-26
        var pattern = @"(?s)(?<UnclosedBlockComment>/\*(?:(?!\*/).)*$)|(?<UnclosedBrackets>\[(?:(?!\]).)*$)";

        var regex = new Regex(pattern, Global.BatchSplittingOptions);
        var matches = regex.Matches(this._rawText);

        if (matches.Count > 0)
        {
            foreach (Match m in matches)
            {
                foreach (Group g in m.Groups)
                {
                    if (g.Success && "_UnclosedBlockComment_UnclosedBrackets".IndexOf(g.Name, StringComparison.InvariantCultureIgnoreCase) > 0)
                        this.SyntaxErrors.Add(new SyntaxError(this.TranslateNonClosedType(g.Name), g.Index, g.Index + g.Length));
                }
            }
        }
    }

    private void Map(string text)
    {
        var pattern = @"(?i)(?s)(?<GoStatement>(?<=\s*)(GO[ \t]+\d+|GO)(?=(\s+|$)))|(?<BlockComment>/\*.*?\*/)|(?<EndOfLineComment>--[^\n\r]*)|(?<String>N?'.*?')|(?<DDLStart>(CREATE|ALTER)\s+)";

        var regex = new Regex(pattern, Global.BatchSplittingOptions);
        var matches = regex.Matches(this._rawText);

        int previousBatchStart = 0;
        foreach (Match m in matches)
        {
            foreach (Group g in m.Groups)
            {
                if (g.Success && "_GoStatement_BlockComment_EndOfLineComment_String_DDLStart".IndexOf(g.Name, StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    switch (g.Name)
                    {
                        case "GoStatement":
                            this.Batches.Add(new Batch(this._rawText.Substring(previousBatchStart, (g.Index + g.Length) - previousBatchStart), g.Value, g.Index, g.Index + g.Length));
                            previousBatchStart = g.Index + g.Length;
                            break;
                        case "BlockComment":
                            this.BlockComments.Add(new BlockComment(g.Value, g.Index, g.Index + g.Length));
                            break;
                        case "EndOfLineComment":
                            this.EolComments.Add(new EolComment(g.Value, g.Index, g.Index + g.Length));
                            break;
                        case "DDLStart":
                            // not sure if I want to try and parse out the object type/name here ... or just get the DDL start and process those AFTER main mapping operations are complete.
                            this.ObjectDeclarations.Add(new ObjectDeclaration(g.Value, g.Index, g.Index + g.Length));
                            break;
                        case "string":
                            // do nothing ... we don't care about strings. They ONLY 'exist' to make sure we don't mis-interpret GO, CREATE/ALTER, or comments, etc. inside them.
                            break;
                    }
                }
            }
        }

        // TODO: if previousBatchStart < text.Length, then we have a final batch to add.

    }

    private string TranslateNonClosedType(string matchName)
    {
        switch (matchName)
        {
            case "UnclosedString":
                return "Non-Terminated String.";
            case "UnclosedBlockComment":
                return "Non-Terminated Block-Comment.";
            case "UnclosedBrackets":
                return "Non-Terminated [object-identifier-within-square-brackets].";
            default:
                throw new NotImplementedException();
        }
    }
}