Set-StrictMode -Version 3.0;

<#

	SIMPLEST EXECUTION OPTION (will find/detect a *.build.sql file in the current working directory (or will throw)): 
		
				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
		#	$global:VerbosePreference = "Continue";
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\simple1");
				Invoke-TsmBuild; 

	MULTIPLE / NESTED INCLUDES:

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\calibration");
				Invoke-TsmBuild; 

	SIMPLE FLAGS-FILE TEST: 

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\conditional");
				Invoke-TsmBuild -Flags "#RDS"; 		
				Write-Host "";
				Invoke-TsmBuild -Flags "#Azure";
	
	DIRECTORY (INCLUDE) EXAMPLE: 
			
				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;		
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\includes");
				Invoke-TsmBuild; 
	
	S4 Build: 

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\");
				Invoke-TsmBuild -CommentDirectives "RemoveHeader";

	SUMMARY and Version (built-in tokens)

				Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
				Set-Location (Get-Location | Split-Path -Parent | Join-Path -ChildPath "\test_files\");
				[string]$version = '14.8.5629.2';  # e.g. pull this from Get-OaToolsSignatureByProject -Project "S4", etc...  
				Invoke-TsmBuild -Version $version -Summary "Standardization of Kill-Store functionality and options; bug fixes" -CommentDirectives "RemoveHeader";

	EXPLICIT EXECUTION OPTION (send in an explicitly defined *.build.sql file(name)): 
				xxxx

	TOKENS (without explicit build-file):



	
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
		[string[]]$Flags,
		#[???[]]$BuildTransforms ... [regex-like-thingies with their matches... ]

		[ValidateSet('RemoveNone', 'RemoveHeader', 'RemoveDoc', 'RemoveEol', 'RemoveBlock', 'RemoveAll')]
		[string[]]$CommentDirectives = @('NotSet')
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		if(-not($PSBoundParameters.ContainsKey('BuildFile'))){
			Write-Verbose "No EXPLICIT -BuildFile specified. Looking for *.build.sql within current working directory.";
			$potentials = @(Get-ChildItem -Path $pwd -Filter "*.build.sql");
			
			switch ($potentials.Count) {
				0 {
					throw "-Build file NOT specified and NO file matching the pattern of `"*.build.sql`" was found in the current directory.`n`tPlease Specify -BuildFile to continue.";
				}
				1 {
					Write-Verbose "	Found [$($potentials[0].Name)]. Assigning as -BuildFile parameter.";
					$BuildFile = $potentials[0].FullName;
				}
				default {
					throw "MULTIPLE files matching the pattern of `"*.build.sql`" were found in the current directory. `n`tPlease Explicitly specify -BuildFile parameter to continue.";
				}
			}
		}
		
		if ([string]::IsNullOrEmpty($BuildFile)) {
			throw "Input Error. No -BuildFile found or specified."; # I don't think we can even get here with the logic above defined as it is... but... meh.
		}
		
		if (-not (Test-Path -Path $BuildFile)) {
			throw "Input Error. Specified -BuildFile: Path not found for [$BuildFile].";
		}
		
		$Flags = @($Flags).Where({ -not [string]::IsNullOrWhiteSpace($_) }).ForEach({ $_.Trim() });
		$bad = $Flags.Where({ -not $_.StartsWith('#') });
		
		if ($bad) {
			throw "Flags MUST start with the '#' sign. Invalid Flags: [$($bad -join ', ')]."
		}
		
		# TODO: if not $Config / $ConfigFile ... then ... look for one via the convention of <build_file_name>.json... 
		# 	so that this can be loaded below... (i.e. don't load it here...  ... here we're just establishing params. )
		# 	unlike -BuildFile ... this can be null/empty but IF specified by the user ... need to run a Test-Path against the path... 
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
		# 			and then INJECT them into $Tokens. 		
		
		# ====================================================================================================
		# Comment-Removal Options:
		# 1) load from .config. 
		# 2) once loaded from .config we'll either have a) ::NotSet or b) a bitmask. 
		# 3) either way, if -CommentDirectives -ne ::NotSet ... then whatever is passed in overwrites "from config" 
		# ====================================================================================================		
		# [tsmake.CommentRemovalDirectives]$commentRemovalDirectives = Load-TokensFromConfig -Config $config;
		[tsmake.CommentRemovalDirectives]$commentRemovalDirectives = [tsmake.CommentRemovalDirectives]::NotSet; # this is JUST a place-holder until 
		
		if ($CommentDirectives -ne 'NotSet') {
			[tsmake.CommentRemovalDirectives]$commentRemovalDirectives = $CommentDirectives;
		}
		
		# ====================================================================================================
		# GO Removal/Cleanup Options
		# ====================================================================================================	
# TODO: think this could be a BuildTRANSFORM option ... i.e., most BUILD transforms are simple REGEXes ... this could be one too. 
		# 		these can NOT be specified via command-line. 
		# 		so if they exist in the config ... copy them over. 
#		$buildGoRemovalDirectives = @();
		
		# ====================================================================================================
		# Build Flags: 
		# ====================================================================================================			
		# TODO: 
		#     Flags are  bit tricky. 
		#     Config files can/should have the option to specify flags. 
		# 		but unlike a number of other 'options' - I'm NOT going to try to and 'blend' or merge -Flags with .Config-Flags. 
		#       instead: 
		#        if $Flags.Count < 1 ... here (nothing was passed in via command-line params)
		#           then I'll look to see if there are any flags in the config. 
		# 		but if we've GOT flags from the command-line... those are the flags we use. 
		#       a WRITE-VERBOSE (at the very minimum) should identify that we're using command-line flags and did NOT load flags from the config. 
		#       and, actually, a) write-verbose here for the above and b) make it pretty obvious in the BUILD-RESULT/output that ... 
		# 			we're using flags from the command-line. 
		#[tsmake.FlagsSource]$flagsSource = [tsmake.FlagsSource]::Console;  # stubb/place-holder for now. 
		
		# ====================================================================================================
		# Build Transforms:
		# ====================================================================================================			
		
		[tsmake.TokenExclusionDirectives]$tokenExclusionDirectives = [tsmake.TokenExclusionDirectives]::None; # CAN ONLY be set via .config values. 
# TODO: map any value other than ::NONE from the .config if provided... 
		
		[tsmake.LineEndingsType]$lineEndingsType = [tsmake.LineEndingsType]::CrLf; # CAN ONLY be set via .config values. 
# TODO: map any value other than ::NONE from the .config if provided... 
		
		Write-Verbose "Configuring Assembler Options.";
		[tsmake.data_models.AssemblerOptions]$assemblerOptions = New-Object tsmake.data_models.AssemblerOptions($tsmTokenRegistry, [tsmake.OperationType]::Build);
		
		[tsmake.data_models.RankedString]$pwdRoot = New-Object tsmake.data_models.RankedString([tsmake.SourceType]::Convention, $pwd);
		$assemblerOptions.AddRootPath($pwdRoot);
		
		# TODO check .config for BOTH root and output paths... 
		
		if (-not ([string]::IsNullOrEmpty($OutputPath))) {
			[tsmake.data_models.RankedString]$cmdLineOutputPath = New-Object tsmake.data_models.RankedString([tsmake.Sourcetype]::CommandLine);
			$assemblerOptions.AddOutputPath($cmdLineOutputPath);
		}
		
		$assemblerOptions.SetDirectives($lineEndingsType, $commentRemovalDirectives, $tokenExclusionDirectives);
		
		if ($Flags.Count -gt 0) {
			[tsmake.FlagsSource]$flagsSource = [tsmake.FlagsSource]::Console;
			$assemblerOptions.SetFlags($Flags, $flagsSource);
		}
		
		Write-Verbose "Starting BUILD. BUILD FILE: [$file]";
		
		## REFACTOR: Because I'm now handling SO MUCH logic within C# and NOT 'stopping' every, friggin, step to check for errors... 
		##    the pipeline is, legit, only around 4-ish REAL lines of code. 
		##    probably JUST fine to roll that into this - i.e., execute those lines down below ... instead of trying to have some big 'pipeline'.
		
		[tsmake.data_models.BuildResult]$buildResult = Execute-Build -BuildFile $BuildFile -BuildOptions $assemblerOptions -WorkingDirectory $pwd;
	};
	
	end {
		
		# NOTE: THIS is attempting to simulate what the xml-format of this object's output will be: (i.e., rough and tumble implementation at this point with no real formatting)
		Write-Host "  Build Stats: Files: $($buildResult.FileCount) CodeLines: $($buildResult.CodeLineCount) Directives: $($buildResult.DirectivesCount)";
		## pretend that this is verbose:
		Write-Host "		Build Root: $($buildResult.BuildRoot) from => $($buildResult.BuildRootSourceType).";
		Write-Host "		Build Flags (via $($buildResult.FlagsSource)): $($buildResult.Flags -join ', ')."
		
		if ($null -ne $buildResult.Exception) {
			Write-Host "Build Exception: $($buildResult.Exception)";
			return;
		}
		
		if ($buildResult.HasErrors) {
			Write-Host "BUILD ERRORS: ";
			
			$buildErrors = $false; # hack
			Write-Host "`tCONFIG ERRORS:";
			foreach ($err in $buildResult.ConfigErrors) {
				Write-Host "`t`t$($err.Summarize())";
				$buildErrors = $true;
			}
			
			if (-not ($buildErrors)) {
				Write-Host "`tSYNTAX ERRORS:";
				foreach ($err in $buildResult.SyntaxErrors) {
					Write-Host "`t`t$($err.Summarize())";
				}
			}
			
			return;
		}
		
		$assembler = $buildResult.Assembler;
		[int]$i = 0;
		foreach ($line in $assembler.ArtifactLines) {
			#Write-Host "$($line.OriginalContent)  => $($line.FileDetail.FilePath)::$($line.LineNumber)";
			Write-Host "$($line.GetContent())";   # $line.OriginalContent
#			if ($null -ne $line.Directive) {
#				if ("FILE" -eq $line.Directive.DirectiveName) {
#					Write-Host "`t`t`t$($line.Directive.FilePath)";
#				}
#			}
#			$i = $i + 1;
#			
#			if ($i -gt 800) {
#				break;
#			}
		}
		
		return $buildResult;
	};
}