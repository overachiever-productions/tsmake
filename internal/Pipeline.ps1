Set-StrictMode -Version 3.0;

function Execute-Pipeline {
	[CmdletBinding()]
	param (
		[ValidateSet("BUILD", "DOCS", "BOTH")]
		[string]$Verb,
		[string]$BuildFile,
		[string]$Output,
		$Version,
		[PSCustomObject]$ConfigData,
		[string[]]$Tokens,
		[PSCustomObject]$Options,
		[string]$WorkingDirectory
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		# new-up a BuildResult object... 
		# and... a file-handler. 
	};
	
	process {
		# ====================================================================================================
		# 1. Create the Build Manifest (i.e., assemble ALL lines of code for processing):
		# ====================================================================================================	
		
		[tsmake.SimpleFileSystem]$fileSystem = New-Object tsmake.SimpleFileSystem($WorkingDirectory);
		[tsmake.Manifest]$manifest = New-Object tsmake.Manifest($fileSystem);
		
Write-Host "doing process stuff... ";
		$manifest.LoadContents($BuildFile);
		
		$manifestLines = $manifest.ManifestLines;
		
		foreach ($line in $manifestLines) {
			#Write-Host "$($line.LineNumber) => $($line.LineText)";
			Write-Host "$($line.LineText)		=> $($line.FileName), $($line.LineNumber)";
		}
		
		
		
		
		
		# Open the -BuildFile as a single, long, string and identify STRINGs and COMMENTS. 
		# 	split by LINE. 
		# 		for each line: 
		# 			- set the lineage/source (i.e. line-number and file source)
		# 			- if it's a directive to open up a new file... then: 
		########			NOTE: 2 types of directives we're looking for here: FILE and DIRECTORY ... 
		# 				- open file as single, long, string and identify STRINGs and COMMENTs + source/lineage. 
		# 				- recurse... 
		# 				- replace line in PARENT with ... this new content. 
		# 
		
		# 	when the above is done we'll have:
		# 		- an array of lines. 
		# 		- where EACH LINE WILL HAVE: 
		# 			- source / lineage 
		# 			- contains and/or IS 'comment' or 'string' 
		
		# 	from the above ... 3 main tasks left: 
		# 		1. for multi-line comments ... look for DOCUMENTATION and such. 
		# 		2. for non-comments: 
		# 			a. look for directives and process (i.e., primarily just versioning and such)
		# 			b. look for and replace tokens as needed. 
		
		
		
	};
	
	end {
		
	};
}