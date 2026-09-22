Set-StrictMode -Version 3.0;

filter Import-Types {
	param (
		[string]$ScriptRoot = $PSScriptRoot
	);
	
	$classFiles = @(
		"$ScriptRoot\dotnet\tsmake\Globals.cs";
		"$ScriptRoot\dotnet\tsmake\Enums.cs";
		"$ScriptRoot\dotnet\tsmake\FileSystem.cs";
		
		"$ScriptRoot\dotnet\tsmake\data_models\Tokens.cs";
		"$ScriptRoot\dotnet\tsmake\data_models\Errors.cs";
		"$ScriptRoot\dotnet\tsmake\data_models\CodeLines.cs";
		"$ScriptRoot\dotnet\tsmake\data_models\AssemblerOptions.cs";
		"$ScriptRoot\dotnet\tsmake\data_models\Results.cs";
		"$ScriptRoot\dotnet\tsmake\data_models\Artifacts.cs";
		
		"$ScriptRoot\dotnet\tsmake\workers\Assembler.cs";
		"$ScriptRoot\dotnet\tsmake\workers\Normalizer.cs";
		"$ScriptRoot\dotnet\tsmake\workers\DocumentationExtractor.cs";
		"$ScriptRoot\dotnet\tsmake\workers\DirectiveProcessor.cs";
		"$ScriptRoot\dotnet\tsmake\workers\TokenTransformer.cs";

#		"$ScriptRoot\dotnet\tsmake\Formatter.cs";
	);
	
	Add-Type -Path $classFiles;
}

Import-Types;

# Internal:
foreach ($file in (@(Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath 'internal/*.ps1') -Recurse -ErrorAction Stop))) {
	try {
		. $file.FullName;
	}
	catch {
		throw "Unable to dot source INTERNAL tsmake file: [$($file.FullName)]`rEXCEPTION: $_  `r$($_.ScriptStackTrace) ";
	}
}

# Public:
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