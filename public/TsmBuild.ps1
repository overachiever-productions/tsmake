Set-StrictMode -Version 3.0;

<#

	SIMPLEST EXECUTION OPTION (will find/detect a *.build.sql file in the current working directory (or will throw)): 
		
				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
			$global:VerbosePreference = "Continue";
				# Set 'current' location = "..\test_files\simple1" 
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\simple1");
				Invoke-TsmBuild; 

	CALIBRATED EXAMPLE:

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
			$global:VerbosePreference = "Continue";
				# Set 'current' location = "..\test_files\calibration1" 
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\calibration1");
				Invoke-TsmBuild; 

	EXPLICIT EXECUTION OPTION (send in an explicitly defined *.build.sql file(name)): 
				xxxx

	TOKENS (without explicit build-file):
				xxxxx

	S4 Build: 
			Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;

			Invoke-TsmBuild -BuildFile "D:\Dropbox\Repositories\S4\Deployment\__build\current.build.sql"
	
#>

function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake", "tsmake")]
	param (
		[string]$BuildFile,
		#[string]$ConfigFile,			# https://overachieverllc.atlassian.net/browse/TSM-30
		#[object]$Config,
		[string]$OutputPath,
		[string]$Version,  
		[string]$Summary,
		[string[]]$Tokens,
		[string[]]$CommentDirectives = @('RemoveHeader') # e.g., -Comments "RemoveHeader", "RemoveDoc", "RemoveEol", "RemoveBlock", "RemoveAll" ... 
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		# TODO: https://overachieverllc.atlassian.net/browse/TSM-32
		if ([string]::IsNullOrEmpty($BuildFile)) {
			Write-Verbose "No EXPLICIT -BuildFile specified. Looking for *.build.sql within current working directory.";
			$potentials = @(Get-ChildItem -Path $pwd -Filter "*.build.sql");
			
			switch ($potentials.Count) {
				0 {
					throw "-Build file NOT specified and NO file matching the pattern of `"*.build.sql`" was found in the current directory.`n`tPlease Specify a -BuildFile to continue.";
				}
				1 {
					Write-Verbose "	Found [$($potentials[0].Name)]. Assigning as -BuildFile parameter.";
					$BuildFile = $potentials[0].FullName;
				}
				default {
					throw "MULTIPLE files matching the pattern of `"*.build.sql`" were found in the current directory. `n`tPlease Explicitly specify -BuildFile parameter input(s) to continue.";
				}
			}
		}
		
		if ([string]::IsNullOrEmpty($BuildFile)) {
			throw "Input Error. No -BuildFile input(s) were found or specified."; # I don't think we can even get here with the logic above defined as it is... but... meh.
		}
		
		if (-not (Test-Path -Path $BuildFile)) {
			throw "Input Error. Specified -BuildFile: Path not found for [$BuildFile].";
		}
	};
	
	process {
		$pwd = Get-Location;
		Write-Verbose "Current Working Directory: [$pwd]";
		
		# ====================================================================================================
		# Config-File(s):
		# ====================================================================================================				
		# TODO: Implement logic for: 
		# 		a. checking for .config file based on -BuildFile name/pattern. 
		# 		b. handling EXPLICIT config file(s) (1 file or multiples)
		# 		c. NO files. (nothing passed in and ...no matches via pattern.)
		
# REFACTOR: 
# ONCE I've found (or not) a .config file ... .then use a PowerShell FUNC to load it. 
#    instead of all of the 'stuff' I've listed below in terms of what to grab - i.e., isolate that into it's own UoW. 
		

		
		# ====================================================================================================
		# Tokens:
		# ====================================================================================================			
		# TODO: 
		# 		implement tokens
		# 			(these are/were working SOLIDLY in v0.3)
		# 			Likewise, the existence of a POTENTIAL .CONFIG file for each of the -BuildFiles that MIGHT be getting processed 
		# 				would be one way to have entirely different tokens from one project to the next AND have some of the tokens that are the same. 
		# 		STEPS: 
		# 				1. reset/unregister any extant tokens (i.e., clear away anything from PREVIOUS runs)
		# 				2. load base/stock/core tokens. 
		# 				3. add in any tokens provided by the command-line and/or via the .config. 	
		# 				4. add in version/summary (below)
		$buildTokens = @();
		
		# ====================================================================================================
		# Version + Summary
		# ====================================================================================================			
		# 	Both of these are JUST tokens. 
		# 	BUT, unlike most tokens - which are static, these guys ... are something that can be EASILY passed-in to each execution.
		# 	
		# 		IF they're present ... i'll do a TINY (or not?) bit of validation... 
		# 			and then INJECT them into $Tokens. 		
		
		# ====================================================================================================
		# Comment-Removal Options:
		# Can be set via .config OR via $CommentDirectives. $CommentDirectives (i.e., CLI) trumps any values from config. 
		# ====================================================================================================		
		[tsmake.CommentRemovalDirectives]$commentRemovalDirectives = [tsmake.CommentRemovalDirectives]::None;
		# TODO: load from config and/or squash via $CommentDirectives
		
		# ====================================================================================================
		# GO Removal/Cleanup Options
		# ====================================================================================================	
# TODO: think this could be a BuildTRANSFORM option ... i.e., most BUILD transforms are simple REGEXes ... this could be one too. 
		# 		these can NOT be specified via command-line. 
		# 		so if they exist in the config ... copy them over. 
#		$buildGoRemovalDirectives = @();
		
		# ====================================================================================================
		# TokenExclusionDirectives:
		# 		CAN ONLY be set via .config values. 
		# ====================================================================================================			
		[tsmake.TokenExclusionDirectives]$tokenExclusionDirectives = [tsmake.TokenExclusionDirectives]::None;
# TODO: map any value other than ::NONE from the .config if provided... 
		
		# ====================================================================================================
		# LineEndingOptions:
		# 		CAN ONLY be set via .config values. 
		# ====================================================================================================	
		[tsmake.LineEndingsType]$lineEndingsType = [tsmake.LineEndingsType]::CrLf;
# TODO: map any value other than ::NONE from the .config if provided... 
		
		Write-Verbose "Starting BUILD. BUILD File: [$file]";
		
		[tsmake.data_models.AssemblerOptions]$assemblerOptions = New-Object tsmake.data_models.AssemblerOptions($tsmTokenRegistry, [tsmake.OperationType]::Build);
		$assemblerOptions.SetOutputPath($OutputPath);
		$assemblerOptions.SetDirectives($commentRemovalDirectives, $commentRemovalDirectives, $tokenExclusionDirectives);
		
		[tsmake.data_models.BuildResult]$buildResult = Execute-Build -BuildFile $BuildFile -BuildOptions $assemblerOptions -WorkingDirectory $pwd;
	};
	
	end {
		Write-Host " Build.Exception: $($buildResult.Exception)";
		Write-Host " Build.HasErrors: $($buildResult.HasErrors)";
		
		Write-Host "  Build Stats: Files: $($buildResult.FileCount) CodeLines: $($buildResult.CodeLineCount)"
		
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
		
		
		return $buildResult;
	};
}