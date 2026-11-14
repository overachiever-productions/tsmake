Set-StrictMode -Version 3.0;

<#

	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
	Import-TsmTokens -TokenStrings "##Version: 14.7.345", "##Summary:Lots of fixin' and stuff";

#>


function Import-TsmTokens {
	[CmdletBinding()]
	#[Alias("")]
	param (
#		[Parameter(Mandatory, Position = 0, ParameterSetName = 'TokensAsObjects')]
#		[ValidateNotNullOrEmpty()]
#		[PSCustomObject]$TokensObject,
		[Parameter(Mandatory, Position = 0, ParameterSetName = 'TokensAsArrayOfStrings')]
		[ValidateNotNullOrEmpty()]
		[string[]]$TokenStrings
#		[Parameter(Mandatory, Position = 0, ParameterSetName = 'SerializedTokens')]
#		[ValidateNotNullOrEmpty()]
#		[string]$Serialized,
#		[switch]$Overwrite = $false					# when $false, tokens already defined can't be overwritten ... 
	);
	
	begin {
		[tsmake.data_models.TokenDefinition[]]$tokens = @();
	};
	
	process {
#		if ($PSBoundParameters.ContainsKey('TokensObject')){
#			Write-Verbose "		Importing Token Objects.";
#			
#			foreach ($keyName in $TokenObject.Keys) {
#				switch ($TokenObject[$keyName].GetType().Name) {
#					"String" {
#						$tokens += New-Object tsmake.models.TokenDefinition($keyName, $TokenObject[$keyName]);
#					}
#					"Hashtable" {
#						$subTable = $TokenObject[$keyName];
#						$buildDefault = $subTable['DefaultBuildValue'];
#						[bool]$allowDefaults = $subTable['AllowInlineDefaults'];
#						[bool]$allowBlanks = $subTable['AllowBlanks'];
#						
#						$tokens += New-Object tsmake.models.TokenDefinition($keyName, "", $buildDefault, $allowDefaults, $allowBlanks);
#					}
#					default {
#						# TODO: this needs better error handling/context. 
#						throw "invalid serialization of token... (TODO: add context about source and ... value in question)"
#					}
#				}
#			}
#		}
		
#		if ($PSBoundParameters.ContainsKey('Serialized')) {
#			Write-Verbose "		Deserializing Tokens.";
#			# deserialize and hand-off to the block below... 
#			# i.e., add to $TokenStrings via @() += 
#		}
		
		if ($PSBoundParameters.ContainsKey('TokenStrings')) {
			Write-Verbose "		Importing Token-Strings.";
			
			foreach ($tokenString in $TokenStrings) {
				Write-Verbose "		Importing Key-Value Pair: [$tokenString] as Token.";
				
				if (-not ($tokenString.Contains(":"))) {
					throw "Invalid Token: [$tokenString]. Serialized Tokens must use 'name:value' syntax.";
				}
				
				$parts = $tokenString.Split(":");
				$tokens += New-Object tsmake.data_models.TokenDefinition($parts[0], $tokenString.Replace($parts[0] + ":", ""));
			}
			
		}
	};
	
	end {
		# https://overachieverllc.atlassian.net/browse/TSM-41 
		#  for the ENCHILADA version, users/callers can construct their OWN 'instance' (singleton) of the Registry ... via helper funcs... 
		#      at which point, not SURE that this func (Import-TsmTokens) will CONTINUE to be what the 'build pipeline' calls to load/populate its EPHEMERAL instance (single build operation only) of a "token collection thingy" (registry)
		#foreach ($token in $tokens) {
		#	$tsmTokenRegistry.SetToken($token, $AllowValueOverride, "USER");
		#}
		# 
		#Write-Verbose "		$($tsmTokenRegistry.DefinedTokens.Count) Token(s) loaded/specified."; 
		
		return $tokens;
	};
}