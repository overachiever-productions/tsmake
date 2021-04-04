using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.GlobalProcessors.Tokens
{
	public class InfoTokenProcessor : BaseProcessor
	{
		public InfoTokenProcessor(IBuildContext buildContext)
		{
			base.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string pattern = TOKEN.INFO.RegularExpression;
			string replacement = this.BuildContext.BuildConfiguration.ProjectInfoText;

			string replaced = Regex.Replace(input, pattern, replacement);

			return replaced;
		}
	}
}