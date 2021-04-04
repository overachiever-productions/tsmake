using tsmake.Interfaces.Factories;

namespace tsmake.Interfaces.Core
{
	public interface IBuildManager
	{
		public IBuildContext BuildContext { get; }

		void ConfigureProcessors(IProcessorFactory processorFactory);
	}
}