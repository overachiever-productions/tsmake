using System;
using System.Text.RegularExpressions;
using tsmake.Interfaces.Core;
using tsmake.Interfaces.Enums;
using tsmake.Interfaces.Processors;
using tsmake.Interfaces.Services;

namespace tsmake.Processors.BuildProcessors.Commands
{
	public class IncludeFileCommand : ICompoundProcessor
	{
		private IProcessor _activeChildProcessor;

		public bool Matched { get; private set; }
		public IProcessor NextProcessor { get; }
		private IBuildContext BuildContext { get; }
		
		public IncludeFileCommand(IBuildContext buildContext)
		{
			this.BuildContext = buildContext;
			this._activeChildProcessor = null;
		}

		public string Process(string input)
		{
			string pattern = COMMAND.INCLUDE.RegularExpression;

			Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Multiline);
			Match m = r.Match(input);

			if (m.Success)
			{
				this.Matched = true;

				string command = m.Value;
				string includePath = command.Split(":")[1].Trim();

				IIncludedFile file = null;
				try
				{
					file = this.BuildContext.FileManager.LoadIncludedFile(includePath);
					this.BuildContext.BuildFile.AddIncludedFile(file);
				}
				catch (Exception ex)
				{

				}

				string processedFileOutput = file.FileContents;


				// FILE HANDLER (most likely) order of operations:
				//		1. replace heading-comments (i.e., HeadingCommentRemovalOperator).  -- multi-line... 
				//		2. CONDITIONAL_SUPPORT  -- multi-line
				//		3. CONDITIONAL_VERSION  -- multi-line
				//		4. NOTEs (i.e., single-line notes).  -- single-line
				//		5. all tokens... -- single-line... 

				try
				{
					while (this._activeChildProcessor != null)
					{
						processedFileOutput = this._activeChildProcessor.Process(processedFileOutput);
						this._activeChildProcessor = this._activeChildProcessor.NextProcessor;
					}
				}
				catch
				{

				}

				string output = input.Replace(command, processedFileOutput);
			}
			
			return input;
		}

		public void ConfigureProcessors(IProcessor childProcessor)
		{
			if (childProcessor == null)
				throw new ConfigurationException("Nested Processors cannot be null/empty.");

			this._activeChildProcessor = childProcessor;
		}
	}



	//public class IncludeFileCommand : BaseProcessor
	//{
	//	private IProcessor _activeProcessor;

	//	public IncludeFileCommand(IBuildContext buildContext)
	//	{
	//		base.BuildContext = buildContext;

	//		base.LineScope = LineScope.SingleLine;
	//		base.ProcessingScope = ProcessingScope.File;
	//		base.SyntaxType = SyntaxType.Command;
	//		base.ProcessorName = this.GetType().Name;
	//	}

	//	public override string Process(string input)
	//	{
	//		string pattern = COMMAND.INCLUDE.RegularExpression;

	//		Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Multiline);
	//		Match m = r.Match(input);

	//		if (m.Success)
	//		{
	//			base.Matched = true;

	//			string command = m.Value;
	//			string includePath = command.Split(":")[1].Trim();

	//			IIncludedFile file = null;
	//			try
	//			{
	//				IIncludedFile file = base.BuildContext.FileManager.LoadIncludedFile(includePath);
	//				base.BuildContext.BuildFile.AddIncludedFile(file);
	//			}
	//			catch (Exception ex)
	//			{

	//			}

	//			string processedFileOutput = file.FileContents;


	//			// FILE HANDLER (most likely) order of operations:
	//			//		1. replace heading-comments (i.e., HeadingCommentRemovalOperator).  -- multi-line... 
	//			//		2. CONDITIONAL_SUPPORT  -- multi-line
	//			//		3. CONDITIONAL_VERSION  -- multi-line
	//			//		4. NOTEs (i.e., single-line notes).  -- single-line
	//			//		5. all tokens... -- single-line... 

	//			try
	//			{
	//				while (this.activeHandler != null)
	//				{
	//					processedFileOutput = this.activeHandler.Process(processedFileOutput);
	//					this.activeHandler = this.activeHandler.NextHandler;
	//				}
	//			}
	//			catch
	//			{

	//			}

	//			string output = input.Replace(command, processedFileOutput);
	//		}

	//		//if (Regex.IsMatch(input, pattern))
	//		//{
	//		//	string fileName = input.Replace(SyntaxDefinitions.Commands.INCLUDE, "");
	//		//	string includePath = Path.Combine(this.BuildContext.ProjectRoot, fileName);

	//		//	try
	//		//	{
	//		//		IIncludedFile file = base.BuildContext.FileManager.LoadIncludedFile(includePath);
	//		//		base.BuildContext.BuildFile.AddIncludedFile(file);
	//		//	}
	//		//	catch(Exception ex)
	//		//	{
	//		//		throw (ex); // TODO implement handling... 
	//		//	}

	//			// if we got a file... 
	//			//		then we need to run the following processors - in the following order: 

	//			//		1. replace heading-comments (i.e., HeadingCommentRemovalOperator).  -- multi-line... 
	//			//		2. CONDITIONAL_SUPPORT  -- multi-line
	//			//		3. CONDITIONAL_VERSION  -- multi-line
	//			//		4. NOTEs (i.e., single-line notes).  -- single-line
	//			//		5. all tokens... -- single-line... 


	//			// once all of the above are done... we've got a 'processed' bit of output to return BACK to the parent/builder... 
	//		//}

	//		return input;
	//	}
	//}
}