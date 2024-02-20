Set-StrictMode -Version 1.0;

function Execute-Pipeline {
	[CmdletBinding()]
	param (
		[PSCustomObject]$BuildContext  # this'll end up being a C# object... 
		# params I'm going to need:
		#  -> build? or docs? or build + docs? - so, basically, a VERB or verbs... maybe an OperationType enum?
		#  -> BuildContext - with things like 
		# 		- file-paths for: input/output. 
		# 		- file-path for CONFIG data - but... the config data can/already-will be defined and imported.
		#       - token-sets (i.e., name-value-pairs for tokens and their explicit values)
		#  -> options - such as: 
		# 		- marker file?  (create one or not - and where?)
		# 		- remove all /* headers */ -or just the first ones? 
		# 		- verbose and debug directives (think these'll just get passed in natively though)
		# 		- strict/stop-on-first-error(s) or ... let errors be processed in 'gulps'
		# -> documentation directives - i.e., specific 'config stuff' for 'transformers and the likes
	);
	
	begin {
		[bool]$xVerbose = ("Continue" -eq $global:VerbosePreference) -or ($PSBoundParameters["Verbose"] -eq $true);
		[bool]$xDebug = ("Continue" -eq $global:DebugPreference) -or ($PSBoundParameters["Debug"] -eq $true);
		
		Reset-PipelineDebugIndents;
		
		Write-Debug "$(Get-PipelineDebugIndent -Key "Root")Starting Pipeline Operations.";
		Write-Verbose "Starting Pipeline Operations.";
		
		[tsmake.BuildResult]$buildResult = New-Object tsmake.BuildResult($BuildContext.BuildFile);  
	};
	
	process {
		# ====================================================================================================
		# 1. Create BuildManifest
		# ====================================================================================================	
		[tsmake.models.BuildManifest]$buildManifest = [tsmake.models.BuildManifest]($BuildContext.BuildFile);
		
		# ====================================================================================================
		# 2. Check-for and Report on (or Handle) and ParserErrors:
		# ====================================================================================================		
		if ($buildManifest.ParserErrors.Count -gt 0) {
			
			# REFACTOR: probably do a better job of handing off errors/details from one model to the next here (i.e., just have buildManifest hand off to results or whatever)
			#  	and then, if we're in a 'fatal' state or found a fatal set of errors, bail... 
			
			# list/process any FATAL errors first
			$fatalErrors = $false;
			foreach($error in $buildManifest.ParserErrors | Where-Object { "FATAL" -eq $_.Severity }) {
				$fatalErrors = $true;
				
				Write-Verbose "Fatal Parsing Error: $($error.ErrorMessage)";
				$buildResult.AddProcessingError($error);
			}
			
			# TODO: MIGHT want to NOT spit these out IF -StopOnFirstError (or whatever I'm going to call it) is set... 
			foreach($error in $buildManifest.ParserErrors | Where-Object { "FATAL" -ne $_.Severity }) {
				$fatalErrors = $true;
				
				Write-Verbose "Parser Warning: $($error.ErrorMessage)";
			}
			
			if ($fatalErrors) {
				return;  # which actually sends us out to the end {} block 
			}
		}
		
		# ====================================================================================================
		# 3. Get / Evaluate Global Directives (ROOT, OUTPUT, FILEMARKER, VERSIONCHECKER, etc.)
		# ====================================================================================================		
		$fatalError = $null;
		if ($null -ne $buildManifest.RootDirective) {
			$root = $buildManifest.RootDirective;
			Write-Verbose "	RootPath Directive found. Path: [$($root.Path)] => PathType: $($root.PathType). Location: $($root.Location.LineNumber), $($root.Location.ColumnNumber).";
			
			switch ($root.PathType) {
				"Absolute" {
					if (-not (Test-Path -Path ($root.Path))) {
						$fatalError = New-ParserError -Severity "Fatal" -Location ($root.Location) -ErrorMessage "Specified Root Path: [$($root.Path)] does NOT exist.";
					}
					
					Write-Verbose "		RootPath Explicitly Set to: [$($root.Path)].";
					# TODO: Set .BuildCOntext.RootPath to ... newly defined root path. 
					# 	only... in order to do the above... i'll need an actual object - with a couple of helper methods or whatever... (i.e., don't want to directly manipulate the object from here).	
				}
				"Relative" {
					$translatedRootPath = Translate-Path -CurrentPath ($BuildContext.WorkingDirectory) -PathDirective ($root.Path);
					
					if (-not (Test-Path -Path $translatedRootPath)) {
						$fatalError = New-ParserError -Severity "Fatal" -Location ($root.Location) -ErrorMessage "Specified Root Path: [$($translatedRootPath)] does NOT exist.";
					}
					
					Write-Verbose "		RootPath Explicitly Set to: [$($translatedRootPath)].";
					
					# TODO: set path (as per above).
				}
				"Rooted" {
					$fatalError = New-ParserError -Severity "Fatal" -Location ($root.Location) -ErrorMessage "Rooted Paths are NOT allowed for RootPath Directives.";
				}
				default {
					$msg = "Unknown PathType: [$($root.PathType)] specified for RootPath Directive.";
					$fatalError = New-ParserError -Severity "Fatal" -Location ($root.Location) -ErrorMessage $msg;
				}
			}
			
			if ($null -ne $fatalError) {
				return;
			}
		}
		
		if ($null -ne $buildManifest.OutputDirective) {
			$outputDirective = $buildManifest.OutputDirective;
			Write-Verbose "	Output Directive found. xxxx";
			
			if ($null -eq $BuildContext.Output) {
				# TODO: 3x tasks:
				# 1. build/define (relative or absolute or rooted) path... 
				# 2. test it. 
				# 3. assign it. 
				#  then... Write-Verbose about how it was set... 
			}
			else {
				Write-Verbose "		-Output Specified via Command Line supersedes ##OUTPUT directive set within .build file.";
			}
			
			if ($null -eq $BuildContext.Output) {
				#$fatalError = New-RuntimeError 
			}
			
			if ($null -ne $fatalError) {
				return;
			}
		}
		
		# TODO: VERSION_CHECKER
		# TODO: FILEMARKER
		# TODO: anything else? 
		
		# ====================================================================================================
		# 4. Process all INCLUDES (FILE/DIRECTORY/RECURSIVE... )
		# ====================================================================================================			
		
		# Start by Validating and Assigning Actual Paths:
		foreach($include in $buildManifest.Directives | Where-Object { $_.DirectiveName -in ("FILE", "DIRECTORY")}){
			if (-not ($include.IsValid)) {
				Write-Host "TODO: Add a parser validation error with line/location and message = $($include.ValidationMessage).";
				# todo add some context to the above... actually, the context will be the line/location and the filename the error came from.
			}
			else {
				# check the path type... 	
				Write-Host "TODO: check/validate actual path + store for: $($include.DirectiveName) -> ($($include.PathType)) $($include.Path)";
				
				# use .PathType + .Path to create the .ConcretePath (or whatever). 
				#  check to see if .ConcretePath is valid. Since we're dealing with files and directories (i.e., includes), each file/directory 
				# 			should exist at this point. 
				# 		if it doesn't, need to add a 'ParserError' (though, hmm... it's not actually a PARSER error)
				# 			maybe there are IProcessingErrors of types ParsingError, Execution/RuntimeError (which is what this'd be), and ... i dunno, logic (dynamic directives, etc.) or whatever errors? 
				# 		yeah... do the above - i.e., have different error types. ParserErrors, RuntimeErrors, Write/OutputErrors, MigrationErrors, Generator(from .git or whatever)Errors, and the likes. 
				# 			they can all use VERY similar, underlying, functionality and interfaces + a base type ... but, they should be different TYPES of errors. 
				
				#  otherwise, IF the .ConcretePath truly exists: 
				# 		$include.SetConcretePath($concretePath)
				# 			which'll also set some sort of .IsReallyValid = true as well... 
			}
			
			# once the above is done... (and assuming that we're not in -EagerFailure = $true (or -StopOnFirstError - i.e., whatever I call it))
			# 		then go ahead and, for each .IsTrulyValid INCLUDE directive... 
			# 			start (recursively) COPYING content from $buildFile (replacement name for $buildManifest) into NewBuildManifest or ExpandoManifest - whatever I'm going to call it.
			# 				where RECURSIVELY means ... for each FileManifest or DirectoryManifest that I create ... pull in the contents, look for FILE/DIRECTORY includes in those... and, if found... recurse on down. 
# 	NOTE: 	FileManifests will be what end up being used for generating IN-FILE documentation. 
# 		 not sure that HAS TO BE processed here (chronologically/logicallty) 
# 		BUT: FileManifests will have processed CommentPreferences (i.e., remove top /* 1x header comment */ or /* all header commnets */
# 			 (where 'removed' means: will NOT have output into the Build/ExpandoManifest ... vs actually deleting/removing any actual text. 
# 				point being: after loading all FILE includes ... i'll have everything i need (within the collection of FileManifests) to grab/parse/build/output IN-FILE docs. 
			# 
			#  	otherwise, once we're done (assuming we didn't run into any errors along the way... )
			# 		we've now got an 'expando' or Build Manifest - something that's ENTIRELY done except for: 
			# 			a) conditional processing/logic.
			# 			b) tokens. 
			# 	translation: 
			# 			from this point on I then need to process: 
			# 		a) conditional logic
			# 		b) tokens
			# 		c) i was going to say: IN-FILE documentation - but, NOPE, that'll have been done up in FileManifests. (Crazy)
			# 		d) writing output to OUTPUT.xxx 
			#   	e) file-marker content/output. 
			# 		f) spitting back results on stats and the overall outcome and the likes. 
			
			
			
			
			
		}
		
		#		foreach ($line in $buildManifest.Lines) {
		#			#write-host "$($line.LineNumber)  -> $($line.LineType)";
		#						
		##			if ($line.LineType.HasFlag([tsmake.enums.LineType]::WhitespaceOnly)) {
		##				Write-Host "$($line.LineNumber)  -> $($line.Content)";
		##			}
		##			if ($line.LineType.HasFlag([tsmake.enums.LineType]::RawContent)) {
		##				if (-not ($line.LineType.HasFlag([tsmake.enums.LineType]::WhitespaceOnly))) {
		##					Write-Host "$($line.LineNumber)  -> $($line.Content)";
		##				}
		##			}
		#			
		#			
		##			if ($line.LineType.HasFlag([tsmake.enums.LineType]::Directive)) {
		##				Write-Host "$($line.LineNumber)  -> $($line.Content)";
		##			}
		#			
		##			if ($line.LineType.HasFlag([tsmake.enums.LineType]::TokenizedContent)) {
		##				
		##				Write-Host "$($line.LineNumber)  -> $($line.Content)";
		##				foreach ($t in $line.Tokens) {
		##					Write-Host "	TokenName: $($t.Name)  -> Value: $($t.DefaultValue)  -> Location: $($t.LineNumber), $($t.Position) ";
		##				}
		##			}
		#		}		
		
		
		
		#		
#		if ($buildManifest.Directives.ContainsKey("ROOT")) {
#			Write-Host "found a ROOT..."
#		}
#		else {
#			Write-Host "didn't find a ROOT"
#		}
		
		$buildResult.SetSucceeded(); # just pretend for now... 
		
		# ====================================================================================================
		# X. ... NEXT
		# ====================================================================================================			
		# these notes replace everything down below. ... 
		# 
		# now that we've gotten a list of ALL directives: 
		# 	1. look for a ROOT. No biggie if we don't have one - it'll be the path/folder where the current .build.sql file is. 
		# 		but, if there is one, try to establish it and so on... i.e., validate and everything. 
		
		
		# 	  1.a ...do the same for ... OUTPUT, FILEMARKER and an others... 
		# 	  1.b Ah yeah... ##VERSION_CHECKER should be processed here - i.e., either there is one and I need to find the code for what was specified. 
		# 			or.... i spam in the tsmake 'default' version-checker ... that'll also, presumably? get dropped at the end? 
		# 	2. foreach LINE in $buildManifest.Lines: 
		# 		a. if the line is a FILE-INCLUDE 
		# 		b or if the line is a DIRECTORY-INCLUDE 
		# 			then, RECURSIVELY, work through addition of any new directives. 
		# 			as in, for each file to be added (not file-include - but each FILE)... 
		# 			pull the contents into a new $fileBuffer or whatever. 
		# 					skim it's contents for INCLUDE (FILE/DIRECTORY) directives... 
		# 					and, if they're present ... include/replace them... over and over and over ... until we're done with 'INCLUDES'
		# 		c. if the line is a ROOT directive... then, skip/continue to the next line (i.e., don't copy into the next 'overall' buffer)
		# 			ditto on things like: ##OUTPUT, ##FILEMARKER and any other 'high-level'/meta-data directives.
		# 		d. if the line is NOT one of the 3x directives above, then ... just copy it out of $buildManifest into $includeExplodedManifest. 
		
		#  at this point, we've got an $includeExplodedManifest with: 
		# 		1. all included directives processed
		# 		2. root processed as well (if there was a ROOT directive)
		# 		3. lines of normal code
		# 		4. tsmake comments - i.e., "COMMENT" directives
		#		5. CONDITIONAL directives. 
		# 			but, what's 'nice' is that all of my conditional directives at this point are ... 'serial' or easy to find/identify. 
		
		# 			meaning that the NEXT and FINAL pass/loop/step is: 
		# 				go through and find/replace all conditional directives with ... dynamic SQL that'll do whatever it needs to to make the build work.
		
		# 	And then, finally: process tokens.
		
		
		
		Write-Host "-------------------------------------------------------";
		
		foreach ($t in $buildManifest.Tokens) {
			# For validation purposes ... need to go through each of these and: 
			#  a) see if it has a default or not. 
			#     if it does, check to see if I've got a TokenDefinition that matches and whether it PREVENTS defaults. 
			# 	  if it does not... see if I've got a TokenDefinition - and if it has a value. (If not, throw.)
			# 		and, actually: don't throw on error. instead, route into a helper func that stores 'parser errors' and ... if -ThrowOnError = $true .. then throw on first (or any) execution
			
			Write-Host "Token Location: $($t.Location.LineNumber), $($t.Location.ColumnNumber) -> TokenName: $($t.Name)  -> DefaultValue: $($t.DefaultValue)  ";
		}
		
		
#		Write-Host "-------------------------------------------------------";
#		Write-Host "Count: $($buildManifest.Directives.Count)"
#		foreach ($d in $buildManifest.Directives) {
#			Write-Host "Directive Location:  $($d.Location.LineNumber), $($d.Location.ColumnNumber) -> Name: $($d.DirectiveName)"
#		}
		
		
		# ====================================================================================================
		# 2. Runtime Validation
		# ====================================================================================================	
		
		# things to validate: 
		# 		file-paths for root/version-checker/file/directory are all valid and can be found (i.e., there's something there)
		# 		if there are ANY version-tokens, make sure that the c# version-object and it's .VersionScheme enum are a correct match for the types of Version Tokens defined. 
		# 		any directives found are well formed and have the correct syntax needed to be validated. 
		# 		for any TOKENS found within the main .build file... make sure that either: tokens allow "", or that we have explicit values - from either .configData or from pipeline. 
		# 			hmm. this one's a bit of a challenge - cuz we can check these for anything in the .build ... but nothing that WILL be included later on has been checked. 
		# 			so... do I check these 2x or .. wait until the end? or ... include everything up front. 
		# 
		
		
	};
	
	end {
		return $buildResult;
	};
}

function Reset-PipelineDebugIndents {
	$script:pipelineIndentManager = New-Object Collections.Generic.List[String];
}

function Get-PipelineDebugIndent {
	param (
		[string]$Key
	);
	
	$Key = $Key.ToLower();
	
	[string]$pad = "";
	[int]$count = 0;
	if ($script:pipelineIndentManager.Contains($Key)) {
		[int]$count = $script:pipelineIndentManager.Count;
		$pad = " ";
		$script:pipelineIndentManager.Remove($Key) | Out-Null;
	}
	else {
		$script:pipelineIndentManager.Add($Key) | Out-Null;
		[int]$count = $script:pipelineIndentManager.Count;
	}
	
	return "$("`t" * ($count - 1))$pad";
}