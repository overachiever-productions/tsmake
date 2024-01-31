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
		
		# TODO: this'll be some sort of C# or MUCH MORE advanced code than this... 
		$content = Get-Content -Path ($BuildContext.BuildFile);
		
		Write-Host $content;
		
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
		
		Write-Host "in ur pipeline doing pipeline stuff.";
		
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