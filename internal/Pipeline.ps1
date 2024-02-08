Set-StrictMode -Version 1.0;

function Execute-Pipeline {
	[CmdletBinding()]
	param (
		[PSCustomObject]$BuildContext  # this'll end up being a C# object... 
		# params I'm going to need:
		#  -> build? or docs? or build + docs? - so, basically, a VERB or verbs... maybe an OperationType enum?
		#  -> BuildContext - with things like 
		# 		- file-paths for: input/output. 
		# 		- file-path for CONFIG data - but... the config data can/already-will be defined and imported.
		#       - token-sets (i.e., name-value-pairs for tokens and their explicit values)
		#  -> options - such as: 
		# 		- marker file?  (create one or not - and where?)
		# 		- remove all /* headers */ -or just the first ones? 
		# 		- verbose and debug directives (think these'll just get passed in natively though)
		# 		- strict/stop-on-first-error(s) or ... let errors be processed in 'gulps'
		# -> documentation directives - i.e., specific 'config stuff' for 'transformers and the likes
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		Reset-PipelineDebugIndents;
		
		Write-Debug "$(Get-PipelineDebugIndent -Key "Root")Starting Pipeline Operations.";
		Write-Verbose "Starting Pipeline Operations.";
		
		# create an errorS (array) object?
	};
	
	process {
		# ====================================================================================================
		# 1. Create BuildManifest
		# ====================================================================================================	
		[tsmake.models.BuildManifest]$buildManifest = [tsmake.models.BuildManifest]($BuildContext.BuildFile);
		
		foreach ($line in $buildManifest.Lines) {
			#write-host "$($line.LineNumber)  -> $($line.LineType)";
						
#			if ($line.LineType.HasFlag([tsmake.enums.LineType]::WhitespaceOnly)) {
#				Write-Host "$($line.LineNumber)  -> $($line.Content)";
#			}
#			if ($line.LineType.HasFlag([tsmake.enums.LineType]::RawContent)) {
#				if (-not ($line.LineType.HasFlag([tsmake.enums.LineType]::WhitespaceOnly))) {
#					Write-Host "$($line.LineNumber)  -> $($line.Content)";
#				}
#			}
			
			
#			if ($line.LineType.HasFlag([tsmake.enums.LineType]::Directive)) {
#				Write-Host "$($line.LineNumber)  -> $($line.Content)";
#			}
			
#			if ($line.LineType.HasFlag([tsmake.enums.LineType]::TokenizedContent)) {
#				
#				Write-Host "$($line.LineNumber)  -> $($line.Content)";
#				foreach ($t in $line.Tokens) {
#					Write-Host "	TokenName: $($t.Name)  -> Value: $($t.DefaultValue)  -> Location: $($t.LineNumber), $($t.Position) ";
#				}
#			}
		}
		
		Write-Host "-------------------------------------------------------";
		
		foreach ($t in $buildManifest.Tokens) {
			# For validation purposes ... need to go through each of these and: 
			#  a) see if it has a default or not. 
			#     if it does, check to see if I've got a TokenDefinition that matches and whether it PREVENTS defaults. 
			# 	  if it does not... see if I've got a TokenDefinition - and if it has a value. (If not, throw.)
			# 		and, actually: don't throw on error. instead, route into a helper func that stores 'parser errors' and ... if -ThrowOnError = $true .. then throw on first (or any) execution
			
			Write-Host "Token Location: $($t.Location.LineNumber), $($t.Location.ColumnNumber) -> TokenName: $($t.Name)  -> DefaultValue: $($t.DefaultValue)  ";
		}
		
		
		Write-Host "-------------------------------------------------------";
		Write-Host "Count: $($buildManifest.Directives.Count)"
		foreach ($d in $buildManifest.Directives) {
			Write-Host "Directive Location:  $($d.Location.LineNumber), $($d.Location.ColumnNumber) -> DirectiveName: $($d.Name)"
		}
		
		
		# ====================================================================================================
		# 2. Runtime Validation
		# ====================================================================================================	
		
		# things to validate: 
		# 		file-paths for root/version-checker/file/directory are all valid and can be found (i.e., there's something there)
		# 		if there are ANY version-tokens, make sure that the c# version-object and it's .VersionScheme enum are a correct match for the types of Version Tokens defined. 
		# 		any directives found are well formed and have the correct syntax needed to be validated. 
		# 		for any TOKENS found within the main .build file... make sure that either: tokens allow "", or that we have explicit values - from either .configData or from pipeline. 
		# 			hmm. this one's a bit of a challenge - cuz we can check these for anything in the .build ... but nothing that WILL be included later on has been checked. 
		# 			so... do I check these 2x or .. wait until the end? or ... include everything up front. 
		# 
		
		
	};
	
	end {
		
	};
}

function Reset-PipelineDebugIndents {
	$script:pipelineIndentManager = New-Object Collections.Generic.List[String];
}

function Get-PipelineDebugIndent {
	param (
		[string]$Key
	);
	
	$Key = $Key.ToLower();
	
	[string]$pad = "";
	[int]$count = 0;
	if ($script:pipelineIndentManager.Contains($Key)) {
		[int]$count = $script:pipelineIndentManager.Count;
		$pad = " ";
		$script:pipelineIndentManager.Remove($Key) | Out-Null;
	}
	else {
		$script:pipelineIndentManager.Add($Key) | Out-Null;
		[int]$count = $script:pipelineIndentManager.Count;
	}
	
	return "$("`t" * ($count - 1))$pad";
}