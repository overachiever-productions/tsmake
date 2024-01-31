using System;
using CommandLine;
using Microsoft.Extensions.Configuration;
using tsmake.FileManagement;
using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Factories;
using tsmake.Interfaces.Processors;
using tsmake.Pipeline;

namespace tsmake.terminal
{
	class Program
	{
		private static IBuildConfig Config;
		//private static ILogger Logger;

		static void Main(string[] args)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += CurrentDomain_UnhandledException;

			var builder = new ConfigurationBuilder()
				.AddJsonFile($"appSettings.json", false, true);

			var config = builder.Build();
			Config = new BuildConfig(config);

			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(Initialize)
				.WithNotParsed(Options.ProcessParserErrors);
		}

		private static void Initialize(Options options)
		{
			//Console.WriteLine("Nice. Running. Inputs: ");
			//Console.WriteLine("\t BuildFile: " + options.BuildFile);
			//Console.WriteLine("\t MajorMinor: " + options.MajorMinor);
			//Console.WriteLine("\t Summary: " + options.VersionSummary);
			//Console.WriteLine("\t Build #: " + options.BuildNumber);
			//Console.WriteLine("Major: " + options.GetMajorVersion());
			//Console.WriteLine("Minor: " + options.GetMinorVersion());
			//Console.WriteLine("\t Config.CopyrightText: " + Config.CopyrightText);

			//Console.ReadKey();
			//return;


			try
			{
				// Initialize Config + BuildContext:
				string currentDirectory = Environment.CurrentDirectory;
				BuildVersion version = new BuildVersion(Config.VersionScheme, options.GetMajorVersion(), options.GetMinorVersion(), options.VersionSummary, options.BuildNumber, Config.VersionCodeDate);
				FileManager fileManager = new FileManager(currentDirectory);

				BuildFile buildFile = new BuildFile(fileManager);
				string buildOutputPath = buildFile.LoadBuildFile(options.BuildFile);

				BuildContext context = new BuildContext(Config, version, buildFile, fileManager);
				
				context.SetProjectRoot(currentDirectory);
				context.SetOutputPath(buildOutputPath);


				// Create a new BuildManager and iterate through processors... 
				IProcessorFactory processorFactory = new ProcessorFactory();

				// this method will create the first processor/type... and chain it to the next... and so on ... until I've created a full-blown pipeline. AND. there's a COMPOUND IProcessor I can/will load for INCLUDE files as well... 
				IProcessor processors = processorFactory.GetOrderedProcessors();  // note that ... i want to do that design pattern thingy of treating a single or MULTIPLE instances of a class as a 'plural'.  

				BuildManager buildManager = new BuildManager(context, processors);

				buildManager.ProcessBuildPipeline();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}

		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if(e.ExceptionObject is Exception ex)
				Console.WriteLine("Unhandled Exception in console application. Unable to LOG details. Exception.Message: " + ex.Message + ".\r\nStackTrace: " + ex.StackTrace);
			else 
				Console.WriteLine("Unhandled Exception in console application. Exception was NULL. Terminating");

			//if (Logger == null)
			//{
			//	if (ex != null)
			//		Console.WriteLine("Unhandled Exception in console application. Unable to LOG details. Execption.Message: " + ex.Message + ".\r\nStackTrace: " + ex.StackTrace);
			//	else
			//		Console.WriteLine("Unhandled Exception in console application. Exeption was NULL.");

			//	return;
			//}

			//if (ex == null)
			//	Logger.AddError("Executable", "Unhandled Exception in console application. Exeption was NULL.");
			//else
			//	Logger.AddError("Executable", "Unhandled Exception in console application. Execption.Message: " + ex.Message + ".\r\nStackTrace: " + ex.StackTrace);

			//if (e.IsTerminating)
			//	Logger.AddError("Engine", "Application is Terminating. Bye.");

		}

	}
}
