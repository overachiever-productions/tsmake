using System.Collections.Generic;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Factories;
using tsmake.Interfaces.Processors;

namespace tsmake.Pipeline
{
	public class BuildManager : IBuildManager
	{
		public IBuildContext BuildContext { get; }

		public BuildManager(IBuildContext buildContext)
		{

		}

		public void ConfigureProcessors(IProcessorFactory processorFactory)
		{
			// create a list of tokenProcessors: 
			List<IProcessor> tokenProcessors = new List<IProcessor>();

			// so... this is brute-force. it works - but only barely. 
			//		a better set of options would be: 
			//			some sort of extension/fluent syntax that makes it a lot easier to 'chain' these things back to back. 
			//	PLUS: i need to somehow put these piglets into play in ORDER... 
			//tokenProcessors.Add(processorFactory.CreateProcessor("some type"));
			//tokenProcessors.Add(processorFactory.CreateProcessor("another type"));
			//tokenProcessors.Add(processorFactory.CreateProcessor("yet another type"));


			// then, build a list of ordered page-level processors - which'll include tokens... 


			// finally, build a list of build-level processors. 
			//    which will ALSO include tokens (they're sorta duplicates)... but they'll only get processed at a per-line level or within each file (on a 'global' level (i.e., full file contents in one 'fell swoop'). 

			// and will also include the FILE_INCLUDE processor - which, as a build-level processor, will include a list of all page-level + token processors. 

			// which sorta means that the IncludeFileToken is kind of both an IProcessor implementation and potentially an IProcessorIterator implementation (i.e., along with the build-manager?)
			//		as in, both will have logic that iterates through processors. 
			//			though, yeah, they'll be different. ish. 
			//			build-level processors will go 'line by line'... 
			//				and have the ability to let each 'line-level' (build-level) processor 'sneak ahead' to address multi-line operations. 
			//		whereas, file-level processors will run full-blown regexes against the entire file 'string' in a single gulp. 


			// which makes me wonder... 
			//		what if i changed the above. 
			//		what if ... build-level processors worked against the ENTIRE build file?
			//			1. OUTPUT could/would replace ... output directive with "" and ... define the output. 
			//			2. TOKEN processors would replace tokens at the build-file level... 
			//			3. NOTE processors would replace ##NOTE: at the build file level with "" 
			//			4. INCLUDE + CONDITIONAL_INCLUDE would ... replace SINGLE LINES with ... gobs of lines. 
			//					5. for each included file: 
			//						a. CONDITIONAL_SUPPORT + CONDITIONAL_XX at the full file-level. 
			//						b. TOKENs.
			//						c. NOTES... 
			//						... done. 

			//			at which point, the build-file (excluding includes (normal and conditional) would be 'built'
			//			and each included file (normal or conditional) would ... be built as well (I'd have to make sure to do token replacement in conditional sp_exec 'alter statement body here' code blocks as well.).

		}
	}
}