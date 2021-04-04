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
			string pattern = COMMAND.NOTE.RegularExpression;

			Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Multiline);
			Match m = r.Match(input);

			if (m.Success)
			{
				base.Matched = true;


				string comment = m.Value.Trim(); // TODO: this could potentially be 0 - N matches... 

				string output = input.Replace(m.Value, string.Empty);

				return output;
			}

			return input;
		}
	}
}