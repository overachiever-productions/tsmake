Set-StrictMode -Version 3.0;

filter Import-Types {
	param (
		[string]$ScriptRoot = $PSScriptRoot
	);
	
	# Import order CAN impact BUILD operations
	$classFiles = @(
		"$ScriptRoot\dotnet\tsmake\Globals.cs";
		"$ScriptRoot\dotnet\tsmake\Enums.cs";
#		"$ScriptRoot\dotnet\tsmake\Errors.cs";
#		"$ScriptRoot\dotnet\tsmake\Extensions.cs";
		
		"$ScriptRoot\dotnet\tsmake\Tokenization\Tokenizer.cs";
		"$ScriptRoot\dotnet\tsmake\Tokenization\Handlers.cs";
		
		"$ScriptRoot\dotnet\tsmake\Directives.cs";
		"$ScriptRoot\dotnet\tsmake\FileProcessing\Manifest.cs";
		
#		"$ScriptRoot\dotnet\tsmake\models\Lines.cs";
#		"$ScriptRoot\dotnet\tsmake\models\Tokens.cs";
#		"$ScriptRoot\dotnet\tsmake\models\Directives.cs";
#		
#		"$ScriptRoot\dotnet\tsmake\models\Files.cs";
#		"$ScriptRoot\dotnet\tsmake\Results.cs";
#		
#		"$ScriptRoot\dotnet\tsmake\Formatter.cs";
	);
	
	Add-Type -Path $classFiles;
}

# Objects: 
Import-Types;

# Private Funcs: 
foreach ($file in (@(Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath 'internal/*.ps1') -Recurse -ErrorAction Stop))) {
	try {
		. $file.FullName;
	}
	catch {
		throw "Unable to dot source INTERNAL tsmake file: [$($file.FullName)]`rEXCEPTION: $_  `r$($_.ScriptStackTrace) ";
	}
}

# Public Funcs: 
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