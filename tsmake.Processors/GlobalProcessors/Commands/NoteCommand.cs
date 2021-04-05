using System;
using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Enums;

namespace tsmake.Processors.GlobalProcessors.Commands
{
	public class NoteCommand : BaseProcessor
	{
		public NoteCommand(IBuildContext buildContext)
		{
			base.BuildContext = buildContext;

			//base.LineScope = LineScope.SingleLine;
			//base.ProcessingScope = ProcessingScope.Global;
			//base.SyntaxType = SyntaxType.Command;
			//base.ProcessorName = this.GetType().Name;
		}

		public override string Process(string input)
		{
			int length = input.Length;
			string pattern = COMMAND.NOTE.RegularExpression;

			string output = Regex.Replace(input, pattern, String.Empty, COMMAND.NOTE.Options);

			if (output.Length < length)
				base.Matched = true;

			return output;
		}
	}
}