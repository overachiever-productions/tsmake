Set-StrictMode -Version 3.0;

<#

	SIMPLEST EXECUTION OPTION (will find/detect a *.build.sql file in the current working directory (or will throw)): 
		
				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
			$global:VerbosePreference = "Continue";
				# Set 'current' location = "..\test_files\simple" 
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\simple1");
				Invoke-TsmBuild; 


	EXPLICIT EXECUTION OPTION (send in an explicitly defined *.build.sql file(name)): 



	MULTI-BUILD VIA PIPELINE (send in multiple files for build/processing): 
		
				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
			$global:VerbosePreference = "Continue";				
				# point to 2x .build files (in the test_files folder): 
				$testFilesBase = (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files");
				$files = @("$($testFilesBase)\simple1\basic.build.sql", "$($testFilesBase)\simple2\my.build.sql");
			
				$files | Invoke-TsmBuild;
			
	MULTI-BUILD VIA PARAMTERS (send in an array of paths as -BuildFile):

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
			$global:VerbosePreference = "Continue";				
				# point to 2x .build files (in the test_files folder): 
				$testFilesBase = (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files");
				$files = @("$($testFilesBase)\simple1\basic.build.sql", "$($testFilesBase)\simple2\my.build.sql");
			
				Invoke-TsmBuild -BuildFile $files;

	TOKENS (without explicit build-file) 

	
#>


function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake", "tsmake")]
	param (
		[Parameter(ValueFromPipeline)]
		[string[]]$BuildFile,
		[string]$ConfigFile,
		[string]$Output,  				

		# Actually... what's the point of having a VERSION here as part of the build process? 
		# do i NEED it? 
		# obviously with things like admindb or ... dda i'm going to want it ... 
		# 	BUT: 
		# 		- how's that work if/when there are > 1 -BuildFiles specified? 
		# 			and... if I can't build multiple projects at the same time when a version is specified ... why bother having a version and/or option to build multiple files? 
		# 		- could I accomplish this in some other way - like with a -Token ??? 
		[string]$Version, 				# TODO: Pass -Version in as a string and have C# code parse it to determing if Semantic, FourPart, or Organic/Custom...
		[string[]]$Tokens,
		[switch]$SkipDocumentation = $false
		# options/switches: 
		# -SkipFileMarker (default is to include one?)
		# -StopOnFirstErrorOrWhatever
		# -NoStats (i.e., skip build/outcome stats like # of lines and # of directives/tokens processed in amount of time processed...)		
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		# TODO: build up an 'Options' object - which will track options for things like: 
		# 		- skip/process file-marker, 
		# 		- remove all /* header comments */ or just the FIRST set. 
		# 		- StopOnFirstError or whatever I'm going to call that feature/option.
		$verb = "BOTH";
		if ($SkipDocumentation) {
			$verb = "BUILD";
		}
		
		$results = @();
	};
	
	process {
		$buildFiles = @();
		$configFiles = @();
		
		$pwd = Get-Location;
		Write-Verbose "Current Working Directory: [$pwd]";
		
		# ====================================================================================================
		# Build File(s):
		# ====================================================================================================				
		foreach ($file in $BuildFile) {
			Write-Verbose "Adding Explicitly Specified -BuildFile: [$BuildFile] for processing.";
			$buildFiles += $file;
		}
		
		if ($buildFiles.Count -eq 0) {
			Write-Verbose "No EXPLICIT -BuildFile specified. Looking for *.build.sql within current working directory.";
			$potentials = @(Get-ChildItem -Path $pwd -Filter "*.build.sql");
			
			switch ($potentials.Count) {
				0 {
					throw "-Build file NOT specified and NO file matching the pattern of `"*.build.sql`" was found in the current directory.`n`tPlease Specify a -BuildFile to continue.";
				}
				1 {
					Write-Verbose "	Found [$($potentials[0].Name)]. Assigning as -BuildFile parameter.";
					$buildFiles += $potentials[0].FullName;
				}
				default {
					# vNEXT: I could put in a switch of -BuildAllMatching or whatever... for this scenario and just .. add each of the matching entries IF specified. 
					throw "MULTIPLE files matching the pattern of `"*.build.sql`" were found in the current directory. `n`tPlease Explicitly specify -BuildFile parameter input(s) to continue.";
				}
			}
		}
		
		if ($buildFiles.Count -lt 1) {
			throw "Configuration Error. No -BuildFile input(s) were found or specified."; # I don't think we can even get here with the logic above defined as it is... but... meh.
		}
		
		# ====================================================================================================
		# Config-File(s):
		# ====================================================================================================				
		# TODO: Implement logic for: 
		# 		a. checking for .config file based on -BuildFile name/pattern. 
		# 		b. checking for the same as above BUT when there are MULTIPLE files. 
		# 			at which point ... ... I'm going to have to 'bind' a -BuildFile and its corresponding -ConfigFile together... 
		# 				i.e., i certainly don't want to have the proj1.build.sql (being built at same time as proj2.build.sql) getting BOUND/'multiplexed' to
		# 					proj2.config.build.sql or whatever... 
		# 						i.e., JUST LINK these via a @{} hash-table - where .. key is -BuildFile and ... value is -ConfigFile. 
		# 		c. handling EXPLICIT config file(s) (1 file or multiples)
		# 		d. NO files. (nothing passed in and ...no matches via pattern.)
		# 			and... ensure that the above works WHEN there are MULTIPLE -BuildFiles. 
		
		# ====================================================================================================
		# Output:
		# ====================================================================================================		
		# 	TODO: 
		# 		Implement. 
		# 				ALSO. 
		# 				there CAN be a SINGLE -Output if/when multiple -BuildFile(s) are specified. 
		# 				the idea would be something along the lines of admindb_latest.sql and admindb_azure_latest.sql ... going to the 
		# 						same output/deployment folder somewhere... 
		# 				i.e., see notes below for VERSION ... 
		# 		OTHERWISE
		# 			in terms of implementation
		# 				all'z I need to do here is verify that the path in question (i.e., -Output) exists. 
		# 				or, more specifically: 
		# 					if -Output is a FOLDER ... and there are multiple -BuildFiles ... we're fine. 
		# 							HOWEVER: the above ONLY works IF each .build.sql file in question has an OUTPUT directive OR a CONFIG-VALUE ... set for the file-name. 
		# 					if -Output is a FILENAME ... and there are multiple -BuildFiles ... i'm pretty sure I have to THROW. 
		# 							PRESUMABLY... if someone sends in a hard-coded path AND multiple -Build files... 
		# 								i COULD 'overwrite' the file-name they've specified by means of the logic for the above (i.e., extract this info from .build.sql or CONFIG-FILE)
		# 								BUT, I THINK that throwing is simply better - i.e., throw "woops - can't specify FILENAME when > 1 -BuildFile. Build 1 file at a time, or use ##output directive or a value in the .Config file instead (for multiples)."
		
		
		# ====================================================================================================
		# Version:
		# ====================================================================================================			
		# TODO: 
		# 		IF there's a -Version specified then: 
		# 			a) if we've got multiple -BuildFile entries... throw. 
		# 				cuz you can't specify the same version for multiple build files ... or... can you? 
		# 				i.e., what would be the use case here... i'm generating ... ah, 2 different build-files or outputs 
		# 					like, say: admindb_latest.sql and admindb_azure_latest.sql 
		# 					and... they need to be set to the SAME version. 
		# 				so. yeah. ignore the notes above. 
		# 				that said, if there are > 1 -BuildFile specified... 
		# 					then do a Write-Verbose "N build files - all will use version XXX".
		# 			b) parse/tease-apart the VERSION to see what kind it is is: Semantic, Four-Part, or Organic/Custom. 
		
		# ====================================================================================================
		# Tokens:
		# ====================================================================================================			
		# TODO: 
		# 		implement tokens
		# 			(these are/were working SOLIDLY in v0.3)
		# 		AND... the SAME tokens CAN be used against multiple -BuildFile inputs/build-files. 
		# 			Likewise, the existence of a POTENTIAL .CONFIG file for each of the -BuildFiles that MIGHT be getting processed 
		# 				would be one way to have entirely different tokens from one project to the next AND have some of the tokens that are the same. 
		# 		STEPS: 
		# 				1. reset/unregister any extant tokens (i.e., clear away anything from PREVIOUS runs)
		# 				2. load base/stock/core tokens. 
		# 				3. add in any tokens provided by the command-line and/or via the .config. 
		
		
		foreach ($file in $buildFiles) {
			#Write-Host "Building: [$file]"			
			
			
			$results += Execute-Pipeline -Verb $verb -BuildFile $file -Output $Output -WorkingDirectory $pwd;
		}
	};
	
	end {
		return $results;
	};
}