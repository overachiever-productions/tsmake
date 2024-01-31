using System.Collections.Generic;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Factories;
using tsmake.Interfaces.Processors;

namespace tsmake.Pipeline
{
	public class BuildManager : IBuildManager
	{
		private IBuildContext BuildContext { get; set; }

		public BuildManager(IBuildContext buildContext)
		{

		}

		public void ProcessBuildPipeline()
		{
			throw new System.NotImplementedException();
		}
	}
}