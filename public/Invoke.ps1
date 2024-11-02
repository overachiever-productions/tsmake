Set-StrictMode -Version 3.0;

<#

	SIMPLEST EXECUTION OPTION (will find/detect a *.build.sql file in the current working directory (or will throw)): 
		
			Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
$global:VerbosePreference = "Continue";
			# Set 'current' location = "..\test_files\simple" 
			Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\simple1");
			Invoke-TsmBuild; 


	EXPLICIT EXECUTION OPTION (send in an explicitly defined *.build.sql file(name)): 



	MULTI-BUILD (send in multiple files for build/processing): 
		
			Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
$global:VerbosePreference = "Continue";				
			# point to 2x .build files (in the test_files folder): 
			$testFilesBase = (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files");
			$files = @("$($testFilesBase)\simple1\basic.build.sql", "$($testFilesBase)\simple2\my.build.sql");
		
			$files | Invoke-TsmBuild;
			

	TOKENS (without explicit build-file) 

	
#>


function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake", "tsmake")]
	param (
		[Parameter(ValueFromPipeline)]
		[string[]]$BuildFile,
		[string]$ConfigFile,
		[string]$Output,  				# TODO: Can't set -Output if/when multiple build-files are PIPED into this func - so... set up a parameter-set accordingly.
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
		
		# TODO: process Version
		
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
		
		Write-Host "building stuff.. ";
		
	};
	
	end {
		
	};
}