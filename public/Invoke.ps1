Set-StrictMode -Version 3.0;

function Invoke-TsmBuild {
	[CmdletBinding()]
	[Alias("Invoke-tsmake", "tsmake")]
	param (
		[Parameter(ValueFromPipeline)]
		[string[]]$BuildFile,
		[string]$ConfigFile,
		[string]$OutputPath,
		[string]$Version,
		[string[]]$Tokens,
		[string[]]$RemoveComments = @('Header'),   # All, Doc, 
		[switch]$GenerateDocs = $true
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		[string[]]$verb = @("BUILD");
		if ($GenerateDocs) {
			$verb += "DOCUMENT";
		}
		
		$buildResult = New-Object tsmake.BuildWrapper;
	};
	
	process {
		
	};
	
	end {
		
	};
}