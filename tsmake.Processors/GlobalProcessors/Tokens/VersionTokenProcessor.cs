using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.GlobalProcessors.Tokens
{
	public class VersionTokenProcessor : BaseProcessor
	{
		public VersionTokenProcessor(IBuildContext buildContext)
		{
			this.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string pattern = TOKEN.VERSION.RegularExpression;
			string replacment = this.BuildContext.BuildVersion.ToString();

			string replaced = Regex.Replace(input, pattern, replacment);

			return replaced;
		}
	}
}