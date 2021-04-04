namespace tsmake.Interfaces.Enums
{
	// defines the scope we TARGET... i.e., does this target a single line - or multiple lines? 
	public enum LineScope
	{
		SingleLine, 
		MultiLine  // i.e., scope of regexes and/or input/output can be 1 or more lines (file-level).
	}
}