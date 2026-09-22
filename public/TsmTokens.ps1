Set-StrictMode -Version 3.0;


# MKC: v2026 ... this is ONLY used to import tokens IF a) they're sent in via command-line (as an object, array-of-strings, or serialized-string) or B) passed in via .config file - in which case I'll deserialize from .config and pass IN to Import-TsmTokens as ... as an array. 

function Import-TsmTokens {
	[CmdletBinding()]
	#[Alias("")]
	param (
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'TokensAsObjects')]
		[ValidateNotNullOrEmpty()]
		[PSCustomObject]$TokensObject,
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'TokensAsArrayOfStrings')]
		[ValidateNotNullOrEmpty()]
		[string[]]$TokenStrings,
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'SerializedTokens')]
		# MKC: v2026. LOL. so, the problem with "serialized" tokens is that they'll need to escape either ticks or commas... or "double-ticks"   e.g., assume this: as a serialization example: "XXX:myXXHere, COPYRIGHT:Copyright 2026, meMyselfAndI, DOCLINK:https/// etc" - the copyright has a "," in the text. I could ... require "around all text" but ... then the string has to be passed in with 'single ticks for the whole serialized string' ... or the user/caller ... etc. 
		[ValidateNotNullOrEmpty()]
		[string]$Serialized,
		[switch]$Overwrite = $false					# when $false, tokens already defined can't be overwritten ... 
	);
	
	begin {
		[tsmake.TokenDefinition[]]$tokens = @();
	};
	
	process {
		if ($PSBoundParameters.ContainsKey('TokensObject')){
			Write-Verbose "		Importing Token Objects.";
			
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
		
		if ($PSBoundParameters.ContainsKey('Serialized')) {
			Write-Verbose "		Deserializing Tokens.";
			# deserialize and hand-off to the block below... 
			# i.e., add to $TokenStrings via @() += 
		}
		
		if ($PSBoundParameters.ContainsKey('TokenStrings')) {
			#Write-Host "ARRAY OF STRINGS: $($TokenStrings.Count)";
			Write-Verbose "		Importing Token-Strings.";
			
			foreach ($tokenString in $TokenStrings) {
				Write-Verbose "		Importing Key-Value Pair: [$tokenString] as Token.";
				
				if (-not ($tokenString.Contains(":"))) {
					throw "Invalid Token: [$tokenString]. Serialized Tokens must use 'name:value' syntax.";
				}
				
				$parts = $tokenString.Split(":");
				$tokens += New-Object tsmake.TokenDefinition($parts[0], $tokenString.Replace($parts[0] + ":", ""));
			}
			
		}
	};
	
	end {
		foreach ($token in $tokens) {
			$tsmTokenRegistry.SetToken($token, $AllowValueOverride, "USER");
		}
		
		Write-Verbose "		$($tsmTokenRegistry.DefinedTokens.Count) Token(s) loaded/specified.";
	};
}