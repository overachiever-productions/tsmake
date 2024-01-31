using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.GlobalProcessors.Tokens
{
	public class CopyrightTokenProcessor : BaseProcessor
	{
		public CopyrightTokenProcessor(IBuildContext buildContext)
		{
			base.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string pattern = TOKEN.COPYRIGHT.RegularExpression;
			string replacement = this.BuildContext.Configuration.CopyrightText;

			string replaced = Regex.Replace(input, pattern, replacement);

			return replaced;
		}
	}
}