Set-StrictMode -Version 3.0;

function Execute-Pipeline {
	[CmdletBinding()]
	param (
		[ValidateSet("Build", "Docs", "BuildAndDocs")]
		[string]$Verb,
		[string]$BuildFile,
		[string]$Output,
		$Version,
		[PSCustomObject]$ConfigData,
		[string[]]$Tokens,
		[PSCustomObject]$Options,
		[string]$WorkingDirectory
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		# new-up a BuildResult object... which... won't HAVE 'much' in terms of 'output'
	};
	
	process {
		# ====================================================================================================
		# 1. Create Core Objects:
		# ====================================================================================================	
		[tsmake.BuildResult]$result = New-Object tsmake.BuildResult($Verb, $BuildFile);
		
		try {
			
			[tsmake.FileSystem]$fileSystem = New-Object tsmake.FileSystem($WorkingDirectory);
			[tsmake.TokenizerFactory]$tokenizerFactory = New-Object tsmake.TokenizerFactory;
			
			[tsmake.Assembler]$assembler = New-Object tsmake.Assembler($fileSystem, $tokenizerFactory);
		}
		catch {
			# new Configuration Error ... 
			# 		and... bind the error to ... the $result. 
			return;
		}
		
		# ====================================================================================================
		# 2. Assemble all source-code from files/sub-files (i.e., includes):
		# ====================================================================================================		
		try {
			$assembler.LoadContents($BuildFile);
			
			
		}
		catch [tsmake.Error] {
			# syntax error or whatever... 
			# 	should be able to just bind it to $results and then:
			return;
		}
		catch {
			# runtime error unless the error is of a specific type... 
			#  bind it to $results and then... 
			return;
		}
		
		
		
		# HACK / TESTING: 
$codeLines = $assembler.CodeLines;
foreach ($line in $codeLines) {
	Write-Host "$($line.LineText)		=> $($line.FileName), $($line.LineNumber)";
}
		
# PICKUP / NEXT: 
		# 1. check for any errors or invalid files. 
		# 		i.e., the $manifest should track these and provide decent context for each problem. 
		# 			e.g., 	 "improper X found here, or there. "
		# 			or, most likely: "file such and such, referenced in blah (where blah is the 'lineage'/stack) ... is not found or not valid/etc. "
		
		# 2. if there weren't any problems, then it's time to run some validations. 
		# 		a. validate all remaining directives. 
		# 		b. validate TOKENs. 
		
		# 3. MIGHT need to do this instead of #2 (i.e., might make sense to flip the order of 3/2 around)
		# 	BUT... time, i think, to 'parse' the assembled contents of $manifest - i.e., have something like $manifest.Get<whatever> ... 
		# 			which returnes a string of the ENTIRE 'body' of the output. 
		# 	PARSE this string. 
		# 		so that I can/will be able to identify: 
		# 			'strings' (don't quite think I care about them... but maybe i do.)
		# 				i mean, in terms of TOKENs, i don't care. 
		# 				BUT ... in terms of DIRECTIVES ... i'm pretty 100% sure I do care - as in, DIRECTIVES can NOT be within N'Strings';
		#			COMMENTS 
		# 				I only care about these for the following reasons: 
		# 					a. documentation 
		# 					b. remove-comment options/directions. 
		
		# 4 At this point... 
		# 		process all tokens? 
		
		# 5. now do ... remaining directives... 
		# 			which'll only be... 
		# 		conditionals, version-checkers, and that's it, right?
		
		
		
		
		# if we get all the way here: 
		#$results.SetComplete();
		# and add in any artifacts as needed... 
	};
	
	end {
		return $result;
	};
}