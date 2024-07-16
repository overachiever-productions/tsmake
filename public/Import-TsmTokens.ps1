Set-StrictMode -Version 1.0;

<#

	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
$global:VerbosePreference = "Continue";

	#Import-TsmTokens -TokenStrings "Key:Value", "X:Y", "Oink: Piggly" #, "I'm bad";

	$rawTokens    = @{
		Copyright = @{
			AllowInlineDefaults = $false
			AllowBlanks		    = $false
			# because there's no DefaultBuildValue here ... this token is required as a command-prompt token... 
		}
		
		Piggly = @{
			AllowInlineDefaults = $false
			AllowBlanks		    = $true
		}

		S4Version_Summary = @{
			AllowInlineDefaults = $false
			AllowBlanks		    = $false
			DefaultBuildValue	= "This is the value that should be specified during the build - unless something is passed in via command-line args."
		}
		
		Doc_Link  = "https://www.somesite.com/docs"
		Project_Link = "https://www.somesite.com"
	}

	Import-TsmTokens -TokenObject $rawTokens;

	$tsmTokenRegistry.DefinedTokens.Count;

#>

function Import-TsmTokens {
	[CmdletBinding()]
	#[Alias("")]
	param (
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'Objects')]
		[PSCustomObject]$TokenObject,
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'Strings')]
		[string[]]$TokenStrings,
		[string]$Source	= "USER-SUPPLIED",				# TODO: integrate this into EXCEPTIONS/ERRORs - so that there's some context on where tokens came from (e.g., COMMAND-LINE | CONFIG-FILE | TSMAKE-CORE)
		[bool]$AllowValueOverride = $false
	);
	
	begin {
		[tsmake.models.TokenDefinition[]]$tokens = @();
	};
	
	process {
		if (Has-ArrayValue $TokenObject) {
			Write-Verbose "		Importing Tokens from serialized objects.";
			
			foreach ($keyName in $TokenObject.Keys) {
				switch ($TokenObject[$keyName].GetType().Name) {
					"String" {
						$tokens += New-Object tsmake.models.TokenDefinition($keyName, $TokenObject[$keyName]);
					}
					"Hashtable" {
						$subTable = $TokenObject[$keyName];
						$buildDefault = $subTable['DefaultBuildValue'];
						[bool]$allowDefaults = $subTable['AllowInlineDefaults'];
						[bool]$allowBlanks = $subTable['AllowBlanks'];
						
						$tokens += New-Object tsmake.models.TokenDefinition($keyName, "", $buildDefault, $allowDefaults, $allowBlanks);
					}
					default {
			# TODO: this needs better error handling/context. 
						throw "invalid serialization of token... (TODO: add context about source and ... value in question)"
					}
				}
			}
		}
		
		if (Has-ArrayValue $TokenStrings) {
			Write-Verbose "		Importing Tokens from serialized strings.";
			
			foreach ($tokenString in $TokenStrings) {
				Write-Verbose "		Importing Key-Value Pair: [$tokenString] as Token.";
				
				if (-not ($tokenString.Contains(":"))) {
					throw "Invalid Token: [$tokenString]. Serialized Tokens must use 'name:value' syntax.";
				}
				
				$parts = $tokenString.Split(":");
				$tokens += New-Object tsmake.models.TokenDefinition($parts[0], $tokenString.Replace($parts[0] + ":", ""));
			}
		}
	};
	
	end {
		if ($tokens.Count -lt 1) {
	# TODO: bolster/etc. 
			throw "Tokens not imported.";
		}
		
		foreach ($token in $tokens) {
			$tsmTokenRegistry.SetToken($token, $AllowValueOverride);
		}
		
		Write-Verbose "		$($tsmTokenRegistry.DefinedTokens.Count) Token(s) loaded/specified.";
	};
}

filter Remove-TsmTokens {
	$tsmTokenRegistry.RemoveTokens();
}

filter Import-TsmBaseFunctionalityTokens {
	
	$tsmakeCoreTokens = @{
		COPYRIGHT = @{
			AllowInlineDefaults = $true
			AllowBlanks = $false
		}
		DOC_LINK  = @{
			AllowInlineDefaults = $true
			AllowBlanks = $true
		}
		
		PROJECT_LINK = @{
			AllowInlineDefaults = $true
			AllowBlanks = $true
		}
		
		MIGRATION_ID = @{
			AllowInlineDefaults = $false
			AllowBlanks		    = $false
		}
		
		VERSION   = @{
			AllowInlineDefaults = $true
			AllowBlanks		    = $false
		}
		
		# TODO: MAJOR, MINOR, BUILD, SEMANTIC ... 
	};
	
	Import-TsmTokens -TokenObject $tsmakeCoreTokens;
}

filter Get-TsmToken {
	param (
		[string]$Name
	);
	
	return $tsmTokenRegistry.GetTokenDefinition($Name);
}