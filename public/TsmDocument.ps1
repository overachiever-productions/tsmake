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
	
	#  NOTE: this'll be the EXACT same-ish workflow as Generating a BUILD (at least there's around 95% overlap in terms of
	#			loading configs, tokens, directives and all of that other stuff... )
	# 			THE ONLY differences will be: 
	# 			 - instead of a 'build', we'll be doing a generate-docs (i.e., like a verb or whatever)
	# 			 - so ... we won't care about #output (we will care about #rootdirectory for includes/etc. )
	# 			 - and we'll need to load a DocExtractor (into the build pipeline)
	#            - and we'll also need to Pass in DocTransformers (which'll have their own equivalents to #output directories, etc.)
	
	# TRANSLATION for the above: 
	# 		1. get Invoke-TsmBuild working as needed - all workflows and such WITHIN the PowerShell func itself. 
	# 		2. figure out how to abstract that logic (i.e., helper fun or whatever that implements the 'guts' )
	#          and then 'overload' that so that it can be used for EITHER running a BUILD or generating DOCs. 
	
	
}