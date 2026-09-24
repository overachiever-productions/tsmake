Set-StrictMode -Version 3.0;

function Execute-Build {
	[CmdletBinding()]
	param (
		[Parameter(Mandatory)]
		[string]$BuildFile,
		[Parameter(Mandatory)]
		[tsmake.data_models.AssemblerOptions]$BuildOptions,
#		[string]$OutputPath,
		[Parameter(Mandatory)]
		[string]$WorkingDirectory
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		[tsmake.data_models.BuildResult]$buildResult = New-Object tsmake.data_models.BuildResult;
	};
	
	process {
		# ====================================================================================================
		# 1. FileSystem, TokenTransformer, ConditionalProcessor, and BuildTransformer
		# ====================================================================================================	
		[tsmake.FileSystem]$fileSystem = New-Object tsmake.FileSystem($WorkingDirectory); # vNEXT++ ... have different IFileSystem implementations for Mac, Linux, etc. 
		[tsmake.workers.OpsFactory]$opsFactory = New-Object tsmake.workers.OpsFactory($fileSystem, $BuildOptions);
		
		# ====================================================================================================
		# 2. Spin up Assembler
		# ====================================================================================================	
		[tsmake.workers.Assembler]$assembler = New-Object tsmake.workers.Assembler($opsFactory, $BuildOptions, $buildResult);
		
		# ====================================================================================================
		# 3. Execute Assembler.Assemble():
		# ====================================================================================================	
		$assembler.Assemble($BuildFile);
	};
	
	end {
		return $buildResult;
	};
}

#function Execute-GenerateDocumentation {
#	
#}