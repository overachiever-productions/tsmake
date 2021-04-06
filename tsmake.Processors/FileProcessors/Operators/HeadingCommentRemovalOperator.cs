using System;
using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;

namespace tsmake.Processors.FileProcessors.Operators
{
	public class HeadingCommentRemovalOperator : BaseProcessor
	{
		
		public HeadingCommentRemovalOperator(IBuildContext buildContext)
		{
			base.BuildContext = buildContext;
		}

		public override string Process(string input)
		{
			string trimmed = input.TrimStart();

			if (trimmed.StartsWith("/*"))
			{
				base.Matched = true;

				string pattern = OPERATOR.HEADER_COMMENT_REMOVER.RegularExpression;

				Regex r = new Regex(pattern, OPERATOR.HEADER_COMMENT_REMOVER.Options);
				string output = r.Replace(input, string.Empty, 1);

				return output;
			}

			return trimmed;
		}
	}
}