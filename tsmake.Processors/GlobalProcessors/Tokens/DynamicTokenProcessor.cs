namespace tsmake.Processors.GlobalProcessors.Tokens
{
	public class DynamicTokenProcessor
	{
		// vNEXT: 
		//	will be able to handle the idea/notion of ... {{##<dynamic_token_name_here>}}
		//			where ...
		//					1x token instance (of this class) will be created for each token defined/configured to run at build time - either in the .config and/or from the command prompt. 
		//				e.g., suppose someone creates a ##CONTEXT_INFO token and a ##SESSION_SOMETHING token... and defines values for them in config and/or command-prompt (no idea how we'd handle these DETAILS)
		//					at runtime, as we build-up a list of tokenProcessors,	
		//						we'd add 2x new token-processors:
		//								- a DynamicTokenProcessor with a name of "token-name-here" (e.g., ##SESSION_SOMETHING)
		//								- a DynamicTokenProcessor with a name of, say, ##CONTEXT_INFO
		//							by default, the values for these would both be pulled from .config. 
		//							but, they COULD have their replacement values overridden by command-line params... 

		//					otherwise, from this point forward, it's just a simple question of ... defining the REGEX, running the pattern + replacement... done. 
	}
}