Set-StrictMode -Version 3.0;

filter Import-Types {
	param (
		[string]$ScriptRoot = $PSScriptRoot
	);
	
	# NOTE: Import order can/does impact BUILD operations
	$classFiles = @(
		"$ScriptRoot\clr\tsmake\Globals.cs";
		"$ScriptRoot\clr\tsmake\Enums.cs";
		"$ScriptRoot\clr\tsmake\Errors.cs";
		"$ScriptRoot\clr\tsmake\Extensions.cs";
		
		"$ScriptRoot\clr\tsmake\models\Lines.cs";
		"$ScriptRoot\clr\tsmake\models\Location.cs";
		"$ScriptRoot\clr\tsmake\models\Tokens.cs";
		"$ScriptRoot\clr\tsmake\models\Directives.cs";
		
		"$ScriptRoot\clr\tsmake\models\Files.cs";
		"$ScriptRoot\clr\tsmake\Results.cs";
		
		"$ScriptRoot\clr\tsmake\Formatter.cs";
	);
	
	Add-Type -Path $classFiles;
}

# Import CLR objects: 
Import-Types;

# Import Private Funcs: 
foreach ($file in (@(Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath 'internal/*.ps1') -Recurse -ErrorAction Stop))) {
	try {
		. $file.FullName;
	}
	catch {
		throw "Unable to dot source INTERNAL tsmake file: [$($file.FullName)]`rEXCEPTION: $_  `r$($_.ScriptStackTrace) ";
	}
}

# Import Public Funcs: 
foreach ($file in (@(Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath 'public/*.ps1') -Recurse -ErrorAction Stop))) {
	try {
		. $file.FullName;
	}
	catch {
		throw "Unable to dot source PUBLIC tsmake file: [$($file.FullName)]`rEXCEPTION: $_  `r$($_.ScriptStackTrace) ";
	}
}

Export-ModuleMember -Function Invoke-TsmBuild, Import-TsmTokens, Get-TsmToken, Remove-TsmTokens;
Export-ModuleMember -Alias *;