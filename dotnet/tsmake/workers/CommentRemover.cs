namespace tsmake.workers;

// Removes T-SQL comments from a WHOLE body of text (e.g., Assembler.ArtifactBody) - per CommentRemovalDirectives:
//      - RemoveLineEndComments:  every -- comment.
//      - RemoveBlockComments:    every /* block comment */ (nested or not).
//      - RemoveAllComments:      both (RemoveAll includes both flags).
//      - RemoveDocComments:      PLACE-HOLDER (see RemoveDocContent) - only when block comments AREN'T being removed (doc comments
//                                are always INSIDE block comments - so removing block comments removes them too).
//      - RemoveHeaderComments:   NOT handled here - header comments are removed per FILE, before directives are processed.
//  Comment markers inside strings, identifiers and OTHER comments are never treated as comments - see Global.CommentsRegex.
//
//  LAYOUT - what's left where a comment used to be:
//      - a line with NOTHING but comments (and whitespace) is removed - line break included.
//      - a comment at the END of a line takes the whitespace before it along:          "SELECT 1;   -- note"  =>  "SELECT 1;"
//      - a comment at the START of a line takes the whitespace after it along:         "    /* note */ SELECT 1;"  =>  "    SELECT 1;"
//      - a comment BETWEEN code leaves exactly one separator (so tokens never merge):   "SELECT /* x */ 1"  =>  "SELECT 1"
//                                                                                       "SELECT/*x*/1"  =>  "SELECT 1"
//      - a block comment's own line breaks go with it:                                  "SELECT a, /* x \r\n y */ b"  =>  "SELECT a, b"
public static class CommentRemover
{
    private static readonly int EolGroup = Global.CommentsRegex.GroupNumberFromName("eol");
    private static readonly int BlockGroup = Global.CommentsRegex.GroupNumberFromName("block");

    public static string RemoveComments(string body, CommentRemovalDirectives directives)
    {
        ArgumentNullException.ThrowIfNull(body);

        bool removeEol = directives.HasFlag(CommentRemovalDirectives.RemoveLineEndComments);
        bool removeBlock = directives.HasFlag(CommentRemovalDirectives.RemoveBlockComments);
        bool removeDoc = !removeBlock && directives.HasFlag(CommentRemovalDirectives.RemoveDocComments);

        // Nothing to do - or nothing to find (cheap, vectorized checks):
        bool findEol = removeEol && body.Contains("--", StringComparison.Ordinal);
        bool findBlock = (removeBlock || removeDoc) && body.Contains("/*", StringComparison.Ordinal);
        if (!findEol && !findBlock)
            return body;

        StringBuilder? output = null;   // created on the FIRST change - i.e., if nothing is removed, body itself is returned.
        int copied = 0;                 // body[0..copied) has been handled - i.e., copied into output (or dropped).

        for (Match match = Global.CommentsRegex.Match(body); match.Success; match = match.NextMatch())
        {
            string replacement;
            if (match.Groups[EolGroup].Success && removeEol)
                replacement = string.Empty;
            else if (match.Groups[BlockGroup].Success && (removeBlock || removeDoc))
            {
                if (removeBlock)
                    replacement = string.Empty;
                else
                {
                    string comment = match.Value;
                    replacement = RemoveDocContent(comment);
                    if (replacement == comment)
                        continue;           // no doc content: unchanged.
                }
            }
            else
                continue;                   // strings, identifiers, unterminated comments and comments we're keeping: unchanged.

            output ??= new StringBuilder(body.Length);
            output.Append(body, copied, match.Index - copied);
            copied = match.Index + match.Length;

            if (replacement.Length > 0)
                output.Append(replacement);     // (only ever a block comment that still has non-doc content.)
            else
                copied = TidyUp(body, output, copied);
        }

        if (output == null)
            return body;

        output.Append(body, copied, body.Length - copied);
        return output.ToString();
    }

    // PLACE-HOLDER: DocComments only ever live INSIDE block comments - so they're handled one block comment at a time (strings,
    //      identifiers and -- comments are already excluded by the regex). The markers that identify doc content (safely) aren't
    //      finalized yet - so, for now, nothing is removed.
    //  Returns: the block comment WITHOUT its doc content (i.e., only the matching lines removed) - or string.Empty when the WHOLE
    //      comment is doc content (it's then removed - and tidied up - like any other comment) - or the comment itself if there's none.
    private static string RemoveDocContent(string blockComment)
    {
        // TODO: identify + remove doc content (once the doc-comment markers are finalized).
        return blockComment;
    }

    // Called right after a comment was removed: output ends where the comment started, and next is the index (in body) just after it.
    //      Applies the LAYOUT rules (see above) - and returns the (possibly advanced) index in body to continue copying from.
    private static int TidyUp(string body, StringBuilder output, int next)
    {
        // BEFORE the comment (on its output line): whitespace only?
        int beforeStart = output.Length;
        while (beforeStart > 0 && IsSpace(output[beforeStart - 1]))
            beforeStart--;
        bool lineStartsHere = beforeStart == 0 || output[beforeStart - 1] is '\r' or '\n';
        bool spaceBefore = beforeStart < output.Length;

        // AFTER the comment (on its line): whitespace only?
        int afterEnd = next;
        while (afterEnd < body.Length && IsSpace(body[afterEnd]))
            afterEnd++;
        bool lineEndsHere = afterEnd == body.Length || body[afterEnd] is '\r' or '\n';
        bool spaceAfter = afterEnd > next;

        if (lineEndsHere)
        {
            output.Length = beforeStart;                // whitespace before the comment goes too (trailing - or, for a comment-only line, indentation).

            if (lineStartsHere)                         // NOTHING else on the line: remove the line (break) too.
            {
                int lineBreak = LineBreakLength(body, afterEnd);
                if (lineBreak > 0)
                    return afterEnd + lineBreak;

                output.Length -= TrailingLineBreakLength(output);      // the LAST line has no break of its own: drop the one before it.
            }

            return afterEnd;
        }

        if (lineStartsHere)                             // code follows: keep the indentation - drop the space between the comment and the code.
            return afterEnd;

        // BETWEEN code: keep exactly one separator.
        if (spaceBefore)
            return afterEnd;

        if (!spaceAfter)
            output.Append(' ');

        return next;
    }

    private static bool IsSpace(char c) => c is not ('\r' or '\n') && char.IsWhiteSpace(c);

    private static int LineBreakLength(string text, int index)
    {
        if (index >= text.Length)
            return 0;

        if (text[index] == '\r')
            return index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;

        return text[index] == '\n' ? 1 : 0;
    }

    private static int TrailingLineBreakLength(StringBuilder text)
    {
        int length = text.Length;
        if (length == 0)
            return 0;

        if (text[length - 1] == '\n')
            return length > 1 && text[length - 2] == '\r' ? 2 : 1;

        return text[length - 1] == '\r' ? 1 : 0;
    }
}
