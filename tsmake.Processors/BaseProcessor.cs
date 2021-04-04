using tsmake.Interfaces.Core;
using tsmake.Interfaces.Enums;
using tsmake.Interfaces.Processors;

namespace tsmake.Processors
{
	public class BaseProcessor : IProcessor
	{
		// Arguably, these details exist MORE for code documentation purposes than they do for functional purposes.
		//protected string ProcessorName { get; set; }
		//protected int ProcessingOrder { get; set; }
		//protected LineScope LineScope { get; set; }
		//protected ProcessingScope ProcessingScope { get; set; }
		//protected SyntaxType SyntaxType { get; set; }
		
		protected IBuildContext BuildContext { get; set; }

		public virtual bool Matched { get; protected set; } = false;

		public virtual IProcessor NextProcessor => throw new System.NotImplementedException();

		public virtual string Process(string input)
		{
			return input;
		}
	}
}