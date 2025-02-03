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
			$result.AddError((New-ConfigurationError -ErrorRecord $_ -Message "Unexpected Error During tsmake Object Initialization." -Phase "tsmake:Startup" -Detail "This is NOT a user error."));
			return;
		}
		
		# ====================================================================================================
		# 2. Assemble all source-code from files/sub-files (i.e., includes):
		# ====================================================================================================		
		try {
			$assembler.LoadContents($BuildFile);
		}
		catch [tsmake.SyntaxException] {
	Write-Host "syntax EXCEPTION (not an error, an EXCEPTION): $_"
			$result.AddError((New-SyntaxError -ErrorRecord $_ -Phase "Pipeline::Assembly" -Facet "Bundling File Contents" -Detail "Assembler.LoadContents(`$BuildFile);" ));
			return;
		}
		catch {
			# runtime error unless the error is of a specific type... 
			#  bind it to $results and then...
	Write-Host "generic error $_"
			return;
		}
		 
		$codeLines = $assembler.CodeLines;
		foreach ($line in $codeLines) {
			Write-Host "$($line.LineText)		=> $($line.FileName) : $($line.LineNumber)  => Stack Depth: $($line.Depth)";
		}
		
		
		
		# when I'm done with 'assembly' ... I should have a collection of .Lines - i.e., every, single, line in the 'assembled' output
		# 		at which point I can: 
		# 		a. run the validations and such (outlined in comments below)
		# 		b. run through each line, one at a time, and process directives. 
		# 			etc... 
		# 			and if I run into any problems - I can - by means of EACH line, report to the user WHICH line it was on - from which FILE ... and the 'stack' or lineage. 
		# 		which means the assembler NEEDs a List<ISourceLine> where ... every, single, ISourceLine can trace its lineage AND 'report on' whether it is: 
		# 			a Directive, has tokens, is a comment ... and/or is a 'header comment' or whatever I'm going to use/allow for 'inline docs'
		
		
		
		# HACK / TESTING: 
#$codeLines = $assembler.CodeLines;
#foreach ($line in $codeLines) {
#	Write-Host "$($line.LineText)		=> $($line.FileName) : $($line.LineNumber)";
#}
		
# PICKUP / NEXT: 
		# 1. check for any errors or invalid files. 
		# 		i.e., the $assembler should track these and provide decent context for each problem. 
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
		
		
		
		
		# if we get all the way here, add in any artifacts as needed... 
		# $result.AddArtifact(xxxx)
	};
	
	end {
		
		
		# HACK: 
		#$result.AddError((New-ValidationError -Message "this is a fake error" -Phase "PRetend::END"));
		
<# 
	Ghetto formatting: 
					Write-Host "SYNTAX ERROR`r`n$_";
						#Write-Host "	Line: $($_.Exception.LineNumber)"
						#Write-Host "	LineOffset: $($_.Exception.LineOffsetStart)"
						#Write-Host "	StartOffset: $($_.Exception.OffsetStart)"
						#Write-Host "	EndOffset: $($_.Exception.OffsetEnd)"
						Write-Host "	File: $($_.Exception.SourceLine.FileName)"; # bug: https://overachieverllc.atlassian.net/browse/TSM-20 
					$stack = [tsmake.StackExtensions]::PrintStack($_.Exception.SourceLine.Stack);
					Write-Host "	$stack"
#>		
		
		$result.SetComplete();
		return $result;
	};
}