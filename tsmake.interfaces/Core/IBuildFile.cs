using System.Collections.Generic;
using tsmake.Interfaces.Services;

namespace tsmake.Interfaces.Core
{
	public interface IBuildFile
	{
		public string RootPath { get; }
		List<string> IncludedFiles { get; }

		void AddIncludedFile(IIncludedFile path);
	}
}