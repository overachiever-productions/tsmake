Set-StrictMode -Version 1.0;

filter Import-Types {
	param (
		[string]$ScriptRoot = $PSScriptRoot
	);
	
	# NOTE: Import order can/does impact BUILD operations
	$classFiles = @(
		"$ScriptRoot\clr\tsmake\enums\VersionScheme.cs"
		
	);
	
	Add-Type -Path $classFiles;
}

Import-Types;