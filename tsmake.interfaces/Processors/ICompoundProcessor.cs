namespace tsmake.Interfaces.Processors
{
	public interface ICompoundProcessor : IProcessor
	{
		void ConfigureProcessors(IProcessor childProcessor);
	}
}