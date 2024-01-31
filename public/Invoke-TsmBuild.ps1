Set-StrictMode -Version 1.0;

<#

	Set-Location "D:\Dropbox\Repositories\tsmake\~~spelunking"

	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
$global:VerbosePreference = "Continue";

	Invoke-TsmBuild;

	#Invoke-TsmBuild -BuildFile "D:\Dropbox\Repositories\tsmake\~~spelunking\current.build.sql"


#>

function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake")]
	param (
		[Parameter(ValueFromPipeline)]
		[string]$BuildFile,
		[string]$ConfigFile,
		[string]$Output, 		# TODO: can't set -Output if/when multiple build-files are PIPED into this func - so... set up a parameter-set accordingly.
		
		[string]$Version   # yeah - this works. Pass it in as a string and have C# code parse it to determing if Semantic, FourPart, or Organic/Custom...
		# options/switches: 
		# -SkipFileMarker (default is to include one?)
		# -SkipDocumentation
		# -StopOnFirstErrorOrWhatever
		
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		# TODO: process Version, Tokens, and other 'global' options that apply to situations where there's 1 build file or MULTIPLE.
		
		# TODO: build up an 'Options' object - that'll track options for things like: 
		# 		- skip/process file-marker, 
		# 		- remove all /* header comments */ or just the FIRST set. 
		# 		- StopOnFirstError or whatever I'm going to call that feature/option.
		
		
		$results = @();
	};
	
	process {
		$pwd = Get-Location;
		
		if (Has-ArrayValue $BuildFile) {
			foreach ($bf in $BuildFile) {
				$cf = Find-ConfigFileByFileNameConvention -BuildFile $bf;
				
				[Hashtable]$cData = @{};
				if ($null -ne $cf) {
					$fn = Split-Path -Path $cf -Leaf;
					Write-Verbose "	Assigning Config-File: [$($fn)] - to Build-File: [$($bf)].";
					
					$cData = Load-ConfigDataFromConfigFile -ConfigFile $cf;
				}
				
				$results += Process-Build -BuildFile $bf -ConfigData $cData -Version $Version -Verbose:$xVerbose -Debug:$xDebug;
			}
		}
		else {
			[Hashtable]$configData = @{};
			
			if (Is-Empty $BuildFile) {
				Write-Verbose "-BuildFile not specified. Looking for single *.build.sql within current (executing) directory: [$($pwd)].";
				
				$potentials = Get-ChildItem -Path $pwd -Filter '*.build.sql';
				
				switch (($potentials).Count) {
					0 {
						throw "-BuildFile not specified and no files matching '*.build.sql' found in current working directory. `n`tPlease specify a -BuildFile to continue.";
					}
					1 {
						Write-Verbose "	Found [$($potentials[0].Name)] - Assigning to -BuildFile.";
						$BuildFile = $potentials[0].FullName;
					}
					default {
						Write-Host "-BuildFile not specified and MORE THAN ONE file matching '*.build.sql' found in current working directory. `n`tPlease specify a single -BuildFile to continue - or PIPE multiple .build.sql files into Invoke-TsmBuild.";
					}
				}
			}
			
			if (Is-Empty $ConfigFile) {
				Write-Verbose "-ConfigFile not specified. Looking for config with name $($buildFileCoreName).build.psd1 or $($buildFileCoreName).build.config.";
				
				$ConfigFile = Find-ConfigFileByFileNameConvention -BuildFile $BuildFile;
				
				if ($null -ne $ConfigFile) {
					$filename = Split-Path -Path $ConfigFile -Leaf;
					Write-Verbose "	Found config file: [$($filename)] - Assigning to -ConfigFile.";
					
					$configData = Load-ConfigDataFromConfigFile -ConfigFile $ConfigFile;
				}
			}
			
			$results += Process-Build -BuildFile $BuildFile -ConfigData $configData -Version $Version -Verbose:$xVerbose -Debug:$xDebug;
		}
	};
	
	end {
		return $results;
	};
}

function Process-Build {
	[CmdletBinding()]
	param (
		[string]$BuildFile,
		[string]$Output,
		[string]$Version,  # this is just a string for now - i.e., initial prototyping... 
		[PSCustomObject]$ConfigData,
		[PSCustomObject]$Options
	)
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
	}
	
	process {
		
		# TODO: eventually going to turn this into a C# class/object... instead of 'loosely typed whatever... '
		[PSCustomObject]$buildContext = [PSCustomObject]@{
			BuildFile = $BuildFile
			Output    = $Output
			Version   = $Version # TODO: this should probably be an object (i.e., C# model) at this point... 
			# Verb? Build | Build+Docs (the option for Docs can only come from Invoke-TsmDocs)
			# Tokens... 
			# Documentation/Transformer Directives
		}
		
		Execute-Pipeline -BuildContext $buildContext -Verbose:$xVerbose -Debug:$xDebug;
		
	}
	
	end {
		return "<TODO: put in some sort of success|failed + context info here... >";
	}
}

filter Find-ConfigFileByFileNameConvention {
	param (
		[string]$BuildFile
	)
	
	$buildFileCoreName = (Split-Path -Path $BuildFile -Leaf).Replace(".build.sql", "");
	
	$potentialConfig = Get-ChildItem -Path $pwd -Filter "$($buildFileCoreName).build.psd1";
	if ($null -eq $potentialConfig) {
		$potentialConfig = Get-ChildItem -Path $pwd -Filter "$($buildFileCoreName).build.config";
	}
	
	return $potentialConfig;
}

filter Load-ConfigDataFromConfigFile {
	param (
		[string]$ConfigFile
	)
	
	try {
		
	}
	catch {
		
	}
}