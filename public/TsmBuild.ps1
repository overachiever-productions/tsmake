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

	TOKENS (without explicit build-file):

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
		# Output:
		# ====================================================================================================	
# TODO: move the logic below into IFileSystem ... it needs to be able to handle the backup/write and other (similar) logic. 
		# and... honestly, no real reason to CHECK/validate -OutputPath at this point as ... it might NOT be specified at all. 		
		# 	UGH... need to move this into the BuildPipeline ... since -OutputPath can/will be NULL at this point. 
		# 	TODO: 
		# 		if -OutputPath is a FOLDER ... and there are multiple -BuildFiles ... we're fine. 
		# 			HOWEVER: the above ONLY works IF each .build.sql file in question has an OUTPUT directive OR a CONFIG-VALUE ... set for the file-name. 
		# 		if -OutputPath is a FILENAME 
		# 			the INTENTION of a BUILD is to ... replace whatever is already in place - i.e., I do this all the time with admindb_latest.sql .. 
		# 				I just overwrite it. 
		# 			So, I'm not sure that there's any justification for:
		# 				- THROW if the file exists. 
		# 				- Requiring something like -Force 
		# 				- Prompting the user to overwrite. 
		# 			BUT, FEATURE-CREEP:
		# 				I can see that if a file already exists...
		# 					 i rename it to xxxx.sql.backup. 
		# 				IF the build fails ... 
		# 					i could revert? 
		# 						or tell users there's a copy.
		# 				IF the build succeeds, then delete .backup... 
		
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
		# Comment-Removal Options
		# ====================================================================================================		
		#  if ... passed in, then ... they overwrite anything found in .config. 
		# 		but ... if nothing was passed in ... look for them in .config... etc. 
		$buildCommentRemovalDirectives = @();
		
		# ====================================================================================================
		# GO Removal/Cleanup Options
		# ====================================================================================================			
		# 		these can NOT be specified via command-line. 
		# 		so if they exist in the config ... copy them over. 
		$buildGoRemovalDirectives = @();
		
		# ====================================================================================================
		# LineEndingOptions:
		# ====================================================================================================	
		# this is a vNext ... if ... at all. 
		# 		but... can only be specified within the .config.
		
		Write-Verbose "Starting BUILD. BUILD File: [$file]";
		
# TODO: might as well turn this into a C# object ... so that I can pass it in to my other models/objects.
		[pscustomobject]$buildOptions = [pscustomobject] @{
			Tokens 							= $buildTokens
			CommentRemovalOptions			= $buildCommentRemovalDirectives
			GoRemovalOptions 				= $buildGoRemovalDirectives
			
			# All of these can be specified via the .Config ... so need to account for them here. 
			#RootPath
			#OutputPath 				# er ... well, if this is in the CONFIG ... then bubble it up to $OutputPath
			#CrLfOptions
		}
		
		$buildResult = Execute-Build -BuildFile $BuildFile -BuildOptions $buildOptions -OutputPath $OutputPath -WorkingDirectory $pwd;
	};
	
	end {
		Write-Host " Build.HasErrors: $($buildResult.HasErrors)"
		
		return $buildResult;
	};
}