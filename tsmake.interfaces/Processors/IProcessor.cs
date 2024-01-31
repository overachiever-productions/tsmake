
namespace tsmake.Interfaces.Processors { 

	public interface IProcessor
	{
		bool Matched { get; }

		IProcessor NextProcessor { get; }

		string Process(string input);
	}


	/*
	 *	FLUENT Additions
	 *		Think what I need for the interface of a Process would be: 
	 *				
	 *			IProcessor .Create(IBuildContext context) (implicit operator stuff and/or returns 'this')
	 *			IProcessor(self/implicit operator)  .AddNextProcessor(suchAndSuchProcessor.Create(context))
	 *				note that AddNextProcessor is DIFFERENT than .NextProcessor() ... .NextProcessor() FETCHES the next one... Add, of course, 'chains' them together. 
	 *
	 *			IProcessor (self/implicit operator) .AddNextComplexProcessor(suchAndSuchProcessor.Create(cont3ext, someParentProcessThatHasAChainOfChildProcessors_AsAnExistingInstanceOrVariable))... 
	 *				not sure I need the method above ... but do need some way to get a 'complexProcessor' loaded/configured.
	*/
}