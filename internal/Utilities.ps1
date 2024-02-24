Set-StrictMode -Version 1.0;

function Write-TsmDebug {
	[CmdletBinding()]
	param (
		[string]$Message #,
		#[switch]$Debug
	);
	
	# ACTUALLY. Might make this public?
	
	# spits stuff out to the console if -Debug
	# always spits stuff out to the PvLog. 
	
}

function Write-TsmVerbose {
	[CmdletBinding()]
	param (
		[string]$Message #,
		#[switch]$Verbose
	);
	
	# TODO: add a 'Verboser' object that is, effectively, an IDENTITY/SEQUENCE - calling it increments. 
	# 		and ... with that, verbose will prefix all calls with # ... as in: 
	# 		0001. Starting up blah blah blah
	# 		0002. doing yada yada
	# 		0003. Compiling xyz... 
	
	
	# ACTUALLY. Might make this public?
	
	# spits stuff out to the console if -Verbose
	# always spits stuff out to the PvLog. 	
}

filter Is-Empty {
	param (
		[Parameter(Position = 0)]
		[string]$Value
	);
	
	return [string]::IsNullOrWhiteSpace($Value);
}

filter Has-Value {
	param (
		[Parameter(Position = 0)]
		[string]$Value
	);
	
	return (-not ([string]::IsNullOrWhiteSpace($Value)));
}

filter Has-ArrayValue {
	param (
		[Parameter(Position = 0)]
		[string[]]$Value # NOTE: any STRING passed in will... be converted to @("string") 
	)
	
	if ($null -eq $Value) {
		return $false;
	}
	
	foreach ($s in $Value) {
		if (Has-Value $s) {
			return $true;
		}
	}
	
	return $false
}

filter Collapse-Arguments {
	param (
		[object]$Arg1,
		[object]$Arg2,
		[switch]$IgnoreEmptyStrings = $false # need to determine IF "" should be output when found... 
	);
	
	if ($Arg1) {
		return $Arg1;
	}
	elseif (-not $IgnoreEmptyStrings) {
		if ((Is-Empty $Arg1)) {
			return $Arg1;
		}
	}
	
	return $Arg2;
}

filter Translate-Path {
	param (
		[Parameter(Mandatory)]
		[string]$CurrentPath,
		[Parameter(Mandatory)]
		[string]$PathDirective
	)
	
	# TODO: SHOULD I be running Test-Path against $CurrentPath to see if it exists? 
	
	$newPath = $CurrentPath;
	$newDirective = $PathDirective;
	
	while ($newDirective.StartsWith('..\')) {
		$newPath = (Get-Item -Path $newPath).Parent.FullName;
		$newDirective = $newDirective.Substring(3);
	}
	
	$output = Join-Path -Path $newPath -ChildPath $newDirective;
	return $output;
}

filter New-FatalParserError {
	param (
		[Parameter(Mandatory)]
		[tsmake.Location]$Location,
		[Parameter(Mandatory)]
		[string]$ErrorMessage
	);
	
	return New-ParserError -Severity "Fatal" -Location $Location -ErrorMessage $ErrorMessage;
}

filter New-ParserError {
	param (
		[Parameter(Mandatory)]
		[tsmake.ErrorSeverity]$Severity,
		[Parameter(Mandatory)]
		[tsmake.Location]$Location,
		[Parameter(Mandatory)]
		[string]$ErrorMessage
	);
	
	return New-Object tsmake.ParserError($Severity, $ErrorMessage, $Location);
}

filter New-BuildError {
	param (
		[tsmake.ErrorSeverity]$Severity = "Fatal",
		[Parameter(Mandatory)]
		[string]$ErrorMessage
	)

}