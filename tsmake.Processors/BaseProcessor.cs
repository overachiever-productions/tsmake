using tsmake.Interfaces.Core;
using tsmake.Interfaces.Processors;

namespace tsmake.Processors
{
	public class BaseProcessor : IProcessor
	{
		protected IBuildContext BuildContext { get; set; }

		public virtual bool Matched { get; protected set; } = false;

		public virtual IProcessor NextProcessor => throw new System.NotImplementedException();

		public virtual string Process(string input)
		{
			return input;
		}
	}
}