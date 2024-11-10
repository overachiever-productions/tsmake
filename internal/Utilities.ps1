Set-StrictMode -Version 3.0;

filter New-ConfigurationError {
	param (
		[Parameter(Mandatory)]
		[System.Management.Automation.ErrorRecord]$ErrorRecord,
		[Parameter(Mandatory)]
		[string]$Message,
		[Parameter(Mandatory)]
		[string]$Phase,
		[tsmake.SourceLine]$SourceLine,
		[string]$Facet,
		[string]$Detail
	);
	
	if ($null -eq $SourceLine) {
		Write-Host "empty source line"
# TODO: need to do something quite a bit different here... 
# and it might, actually, make more sense to new-up a fake 'sourceLine' in the FEW areas where i won't, obviously/naturally, have an ISourceLine. 		
		$stack = New-Object System.Collections.Generic.Stack[string];
		$SourceLine = New-Object tsmake.SourceLine(11, "fake file", "some text as the body", 3, $stack);
	}
	
	return [tsmake.Error]::FakeError($ErrorRecord);
	##return [tsmake.Error]::NewConfigurationError($ErrorRecord, $SourceLine, $Phase, $Message, $Facet, $Detail);
}

