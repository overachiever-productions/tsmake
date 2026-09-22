Set-StrictMode -Version 3.0;

function Invoke-TsmDocument {
	[CmdletBinding()]
	[Alias("Invoke-tsdoc", "tsdoc", "document")]
	param (
		[Parameter(ValueFromPipeline)]
		[string[]]$BuildFile,
		#[string]$ConfigFile,			# SEE: https://overachieverllc.atlassian.net/browse/TSM-30
		#[string]$OutputPath,			# I THINK each transformer will specify its output ... vs a single ... output per each .build or whatever.
		[PsObject[]]$Transformers,
		# i.e., 1 or more transformers or 'targets' to use ... NOT sure, yet, what the data-type will be. I could pass these in as NAMES or ... as objects...
		[string]$Version,
		[string[]]$Tokens
	);
}