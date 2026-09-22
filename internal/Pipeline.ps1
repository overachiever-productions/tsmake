Set-StrictMode -Version 3.0;

function Execute-Build {
	[CmdletBinding()]
	param (
		[Parameter(Mandatory)]
		[string]$BuildFile,
		[Parameter(Mandatory)]
		[pscustomobject]$BuildOptions,
		[string]$OutputPath,
		[Parameter(Mandatory)]
		[string]$WorkingDirectory
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		[tsmake.data_models.BuildResult]$buildResult = New-Object tsmake.data_models.BuildResult;
	};
	
	process {
		# NOTE:
		# 	-OutputPath can (and will - in many cases) BE NULL/EMPTY. 
		# 		i.e., it'll typically be specified EITHER by the CONFIG (in which case it'll be passed in from above - just as if it were specified by the command-line)
		# 			OR it can/will be specified by the ##OUTPUT directive within the build.sql itself. 		
		
		# ====================================================================================================
		# X. Convert -BuildFile to ... an ICodeFile or whatever. as a concrete type of BUILD. 
		# ====================================================================================================	
		# 	NOPE ... pass a file-path into IAssembler. 
		# 			it manages all files. 
		# 			IFile details are NEVER visible to powershell. 
		
		# ====================================================================================================
		# 1. FileSystem, TokenTransformer, ConditionalProcessor, and BuildTransformer
		# 		NOTE: I do NOT need a Doc'Grabber' - i.e., the UoW/Class that would GRAB DocBlocks from each file during processing. 
		# 			that said, the ASSEMBLER will delegate to a DocGrabber during DOC builds ... so, the IAssembler needs a .ctor that allows this UoW/Class to be passed in. 
		# ====================================================================================================	
		[tsmake.FileSystem]$fileSystem = New-Object tsmake.FileSystem($WorkingDirectory);
		
		# ====================================================================================================
		# 2. Spin up Assembler
		# ====================================================================================================	
		
		# .ctor for the Assembler will require: 
		# 		-1. Probably a MODE switch for BUILD vs DOC ... i.e., there's a ton of overlap... but ... some things can/will be skipped and/or useless in one mode or the other. 
		# 		0. The $buildResult - it'll be shoving errors into this. 
		# 			or maybe i just copy assembler.errors into $build.Result.AddErrors($xx) or whatever. 
		# 		A. Unit-of-Work objects: TokenTransformer, ConditionalProcessor, BuildTransformer. 
		# 				i.e., poor-man's IoC ... spin them up here, pass them in. 
		#		B. Tokens
		# 		C. GoCleanupDirectives
		# 		D. CommentRemovalDirectives
		# 		E. PATHS: ##root-path, ##output-path (if specified by either the config and/or as -parameter(s))
		
		# ====================================================================================================
		# 3. Assembler.BUILD($buildFIle)
		# ====================================================================================================	
		
		#  ... at this point I've got:
		# 		syntax errors and/or an EXCEPTION. 
		# 		or 
		# 		- syntax errors
		# 		- ##root-path and ##output-path directives ... (if they weren't already defined by the config)
		#   	- an ASSEMBLED 'code base'
		# 				all includes inlined. 
# woah ... i don't need to collect these IF I'm JUST BUILDING. 
		# 				all DOC comments captured
		# 				header comments removed. 
		# 			directives identified
		# 			tokens identified? (think so)
		# 			conditionals identified (yeah - they're tokens)
		# 			full object model interaction with all of the above. 
		
		$buildResult.SetComplete();
	};
	
	end {
		return $buildResult;
	};
}

#function Execute-GenerateDocumentation {
#	
#}