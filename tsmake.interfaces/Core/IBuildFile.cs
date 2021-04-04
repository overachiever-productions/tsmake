using System.Collections.Generic;
using tsmake.Interfaces.Services;

namespace tsmake.Interfaces.Core
{
	public interface IBuildFile
	{
		public string RootPath { get; }
		//List<int> ProcessedLines { get; }
		List<string> IncludedFiles { get; }

		//string GetContentsStartingAtLine(int lineNumber);

		//bool HasLineBeenProcessed(int lineNumber);
		void AddIncludedFile(IIncludedFile path);
		//void MarkLineAsProcessed(int lineNumber);
	}
}