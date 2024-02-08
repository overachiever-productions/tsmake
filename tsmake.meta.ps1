Set-StrictMode -Version 1.0;

filter Import-Types {
	param (
		[string]$ScriptRoot = $PSScriptRoot
	);
	
	# NOTE: Import order can/does impact BUILD operations
	$classFiles = @(
		"$ScriptRoot\clr\tsmake\enums\VersionScheme.cs";
		"$ScriptRoot\clr\tsmake\enums\LineType.cs";
		
		"$ScriptRoot\clr\tsmake\models\Location.cs";
		"$ScriptRoot\clr\tsmake\models\TokenDefinition.cs";
		"$ScriptRoot\clr\tsmake\models\directives\DirectiveInstance.cs";
		"$ScriptRoot\clr\tsmake\models\directives\DirectiveFactory.cs";
		"$ScriptRoot\clr\tsmake\models\TokenInstance.cs";
		
		"$ScriptRoot\clr\tsmake\models\Line.cs";
		
		"$ScriptRoot\clr\tsmake\models\TokenRegistry.cs";
		"$ScriptRoot\clr\tsmake\models\BuildManifest.cs";
		
	);
	
	Add-Type -Path $classFiles;
}

Import-Types;