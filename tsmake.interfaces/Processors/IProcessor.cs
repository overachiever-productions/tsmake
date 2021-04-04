using tsmake.Interfaces.Core;
using tsmake.Interfaces.Enums;

namespace tsmake.Interfaces.Processors { 

	public interface IProcessor
	{
		bool Matched { get; }

		IProcessor NextProcessor { get; }

		string Process(string input);
	}
}