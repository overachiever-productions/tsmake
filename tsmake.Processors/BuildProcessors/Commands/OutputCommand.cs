using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.BuildProcessors.Commands
{
	public class OutputCommand : BaseProcessor
	{
		public OutputCommand(IBuildContext buildContext)
		{
			this.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string pattern = COMMAND.OUTPUT.RegularExpression;

			Regex r = new Regex(pattern, COMMAND.OUTPUT.Options);
			Match m = r.Match(input);
			if (m.Success)
			{
				base.Matched = true;

				string command = m.Value;
				string outputPath = command.Split(":")[1].Trim();
				this.BuildContext.SetOutputPath(outputPath);

				string output = input.Replace(command, string.Empty);

				return output;
			}

			return input;
		}
	}
}