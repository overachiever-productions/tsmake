Set-StrictMode -Version 1.0;

# ====================================================================================================
# BuildContext
# 		I MAY turn BuildContext into a CLR object - IF it ends up having enough 'logic' within funcs/etc.
# ====================================================================================================	
function New-BuildContext {
	param (
		[parameter(Mandatory)]
		[string]$BuildFile,
		[string]$Output,
		[string]$Version,
		[parameter(Mandatory)]
		[string]$WorkingDirectory,
		[parameter(Mandatory)]
		[ValidateSet("BUILD", "DOCS", "BOTH")]
		[string]$Verb
	)
	
	begin {
		[ScriptBlock]$setOutputLocation = {
			param (
				[Parameter(Mandatory)]
				[string]$OutputLocation
			);
			
			$this.Output = $OutputLocation;
		}
		
		[ScriptBlock]$setRootLocation = {
			param (
				[Parameter(Mandatory)]
				[string]$RootLocation
			);
			
			$this.Root = $RootLocation;
		}
	}
	
	process {
		[PSCustomObject]$context = [PSCustomObject]@{
			BuildFile	     		= $BuildFile
			Output		     		= $Output
			Version		     		= $Version 				# TODO: this should probably be an object (i.e., C# model) at this point... 
			WorkingDirectory 		= $WorkingDirectory
			Root			 		= "" # Build Root. Set later/explicitly.
			Verb 			 		= $Verb
			
			# Documentation/Transformer Directives
		}
		
		Add-Member -InputObject $context -MemberType ScriptMethod -Name SetOutput -Value $setOutputLocation;
		Add-Member -InputObject $context -MemberType ScriptMethod -Name SetRoot -Value $setRootLocation;
	}
	
	end {
		return $context;
	}
}

# ====================================================================================================
# Token Registry
# ====================================================================================================	
$global:tsmTokenRegistry = [tsmake.models.TokenRegistry]::Instance;

# ====================================================================================================
# Formatter
# ====================================================================================================	
$global:TsmFormatter = [tsmake.Formatter]::Instance;
$TsmFormatter.SetCurrentHostInfo($Host.Name, $Host.UI.RawUI.WindowSize.Width);