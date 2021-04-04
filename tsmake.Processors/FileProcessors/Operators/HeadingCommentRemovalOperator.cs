using tsmake.Interfaces.Core;
using tsmake.Interfaces.Enums;

namespace tsmake.Processors.FileProcessors.Operators
{
	public class HeadingCommentRemovalOperator : BaseProcessor
	{
		
		public HeadingCommentRemovalOperator(IBuildContext buildContext)
		{
			base.BuildContext = buildContext;

			//base.LineScope = LineScope.MultiLine;
			//base.ProcessingScope = ProcessingScope.File;
			//base.SyntaxType = SyntaxType.Operator;
			//base.ProcessorName = this.GetType().Name;
		}

		public override string Process(string input)
		{
			// SCOPE: Doesn't attempt to remove all multi-line comments - just the first set IF it's above CREATE/ALTER statements... 
			return base.Process(input);
		}
	}
}