using tsmake.Interfaces.Core;
using tsmake.Interfaces.Processors;

namespace tsmake.Interfaces.Factories
{
	public interface IProcessorFactory
	{
		// need to work on this interface more... it needs some way to 'switch' or 'case' out what to grab. 
		//		but, what it has to have is the context - as it'll inject that into the processor. 
		IProcessor CreateProcessor(string name, IBuildContext context);
	}
}