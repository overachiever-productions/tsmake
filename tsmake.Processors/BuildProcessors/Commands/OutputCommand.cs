using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

// TODO: the logic for loading/getting the --##OUTPUT: directive has been shoved into the BuildFile (IBuildFile) 's .LoadBuildFile() method cuz... it's so critical to overall config/workflow that we get that info up-front. 
//		which means that either: a) I remove this object as a processor or b) I rework IBuildFile.LoadBuildFile() to use a processor instead... 
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