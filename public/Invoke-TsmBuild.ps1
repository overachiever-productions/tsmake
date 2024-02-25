Set-StrictMode -Version 1.0;

<#

	Set-Location "D:\Dropbox\Repositories\tsmake\~~spelunking";

	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
$global:VerbosePreference = "Continue";

	Invoke-TsmBuild -Tokens "Doc_Link:https://www.overachiever.net";

	Get-TsmToken -Name "Doc_Link";



	#$files = @("D:\Dropbox\Repositories\tsmake\~~spelunking\current.build.sql", "D:\Dropbox\Repositories\S4\Deployment\__build\current.build.sql);
	#$files | Invoke-TsmBuild;
#>

function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake")]
	param (
		[Parameter(ValueFromPipeline)]
		[string]$BuildFile,
		[string]$ConfigFile,
		[string]$Output, 		# TODO: Can't set -Output if/when multiple build-files are PIPED into this func - so... set up a parameter-set accordingly.
		[string]$Version, 		# TODO: Pass it in as a string and have C# code parse it to determing if Semantic, FourPart, or Organic/Custom...
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
		
		# THEN: if $Tokens isn't empty... pass contents of $Tokens into Import-TsmTokens
		# 		where, down at the end{} part of the func... will add each token into the TokenRegistry if it DOESN'T already exist, and, if it does, will set the value... 

		
		# TODO: build up an 'Options' object - that'll track options for things like: 
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
		$pwd = Get-Location;
		
		if (Has-ArrayValue $BuildFile) {
			Write-Verbose "Multiple Files...";
			foreach ($bf in $BuildFile) {
				$cf = Find-ConfigFileByFileNameConvention -BuildFile $bf;
				
				[Hashtable]$cData = @{};
				if ($null -ne $cf) {
					$fn = Split-Path -Path $cf -Leaf;
					Write-Verbose "	Assigning Config-File: [$($fn)] - to Build-File: [$($bf)].";
					
					$cData = Load-ConfigDataFromConfigFile -ConfigFile $cf -Verbose:$xVerbose -Debug:$xDebug;
				}
				
				$results += Process-Build -BuildFile $bf -ConfigData $cData -Version $Version -Verb $verb -Verbose:$xVerbose -Debug:$xDebug;
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
			else {
				# TODO: Run Test-Path and throw if the file specified doesn't exist or ... whatever
			}
			
			if (Is-Empty $ConfigFile) {
				Write-Verbose "-ConfigFile not specified. Looking for config with name $($buildFileCoreName).build.psd1 or $($buildFileCoreName).build.config.";
				$ConfigFile = Find-ConfigFileByFileNameConvention -BuildFile $BuildFile;
			}
			
			[PSCustomObject]$configData = $null;
			if (Has-Value $ConfigFile) {
				if (-not (Test-Path -Path $ConfigFile)) {
					throw "Invalid -ConfigFile specified. Path Not Found: [$ConfigFile].";
				}
				
				$filename = Split-Path -Path $ConfigFile -Leaf;
				Write-Verbose "	Found config file: [$($filename)] - Assigning to -ConfigFile.";
				
				$configData = Load-ConfigDataFromConfigFile $ConfigFile -Verbose:$xVerbose -Debug:$xDebug;
			}
			
			$results += Process-Build -BuildFile $BuildFile -ConfigData $configData -Version $Version -Tokens $Tokens -Verb $verb -Verbose:$xVerbose -Debug:$xDebug;
		}
	};
	
	end {
		return $results;
	};
}

# REFACTOR: This underlying 'helper' func can/will be what BOTH Invoke-TsmBuild and Invoke-TsmDocs call into - the ONLY difference between calls 
# 		will be the -Verb { DOCS | SQL | BOTH }
function Process-Build {
	[CmdletBinding()]
	param (
		[string]$BuildFile,
		[string]$Output,
		[string]$Version,  # this is just a string for now - i.e., initial prototyping... 
		[PSCustomObject]$ConfigData,
		[string[]]$Tokens,
		[PSCustomObject]$Options,
		[ValidateSet("BUILD", "DOCS", "BOTH")]
		[string]$Verb
	)
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		Remove-TsmTokens;
	}
	
	process {
		
		if (Has-Value $ConfigData) {
			$configFilePath = $ConfigData.ConfigDataSource;
			
			Write-Verbose "	Leveraging Config Data from file: [$configFilePath].";
			
			# TODO: Address options for things like Root, Output, FileMarker, Comment-Removal, and the likes into the BuildContext and/or BuildOptions objects.
			
			if (Has-Value $ConfigData['TokenDefinitions']) {
				[PSCustomObject]$configFileTokens = $ConfigData['TokenDefinitions'];
				
				Import-TsmTokens -TokenObject $configFileTokens -Source "CONFIG-FILE: $configFilePath" -AllowValueOverride $false;
			}
		}
		
		if ($null -ne $Tokens) {
			Write-Verbose "-Tokens being set from command-line input.";
			Import-TsmTokens -TokenStrings $Tokens -Source "COMMAND-LINE: $Tokens" -AllowValueOverride $true;
		}
		
		[PSCustomObject]$buildContext = New-BuildContext -BuildFile $BuildFile -Output $Output -Version $Version `
			-WorkingDirectory (Get-Location) -Verb $Verb -Verbose:$xVerbose -Debug:$xDebug;;
		
		$result = Execute-Pipeline -BuildContext $buildContext -Verbose:$xVerbose -Debug:$xDebug;
	}
	
	end {
		return $result;
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

function Load-ConfigDataFromConfigFile {
	param (
		[Parameter(Mandatory)]
		[string]$ConfigFile
	);
	
	$configData = Import-PowerShellDataFile $ConfigFile;
	
	# TODO: figure out best way to THROW if/when there's an error here. 
	# 	or if there's no data.  i.e., just a simple throw (with try/catch in the callers? or ... should this
	# 		instead be a RuntimeError? (should probably be a runtime error) or maybe a ConfigError - or whatever.)
	if (Has-Value $configData) {
		# VALIDATION: 
		# TODO: need to figure out which sections are 'required or not... ' and... honestly, I don't think ANY of them are 'required'.
		
		$configData | Add-Member -MemberType NoteProperty -Name ConfigDataSource -Value $filename -Force;
	}
	
	return $configData;
}