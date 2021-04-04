using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.GlobalProcessors.Tokens
{
	public class VersionSummaryTokenProcessor : BaseProcessor
	{
		public VersionSummaryTokenProcessor(IBuildContext buildContext)
		{
			this.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string pattern = TOKEN.VERSION_SUMMARY.RegularExpression;
			string replacement = this.BuildContext.BuildVersion.VersionSummary;

			string replaced = Regex.Replace(input, pattern, replacement);

			return replaced;
		}
	}
}