namespace tsmake.Interfaces.Configuration
{
	public interface IBuildConfig
	{
		// might break these out into a distinct config in the future: 
		string CopyRightText { get; }
		string ProjectInfoText { get; }
	}
}