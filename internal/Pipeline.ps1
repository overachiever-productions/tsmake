Set-StrictMode -Version 1.0;

function Execute-Pipeline {
	[CmdletBinding()]
	param (
		[PSCustomObject]$BuildContext  # this'll end up being a C# object... 
		# params I'm going to need:
		#  -> build? or docs? or build + docs? - so, basically, a VERB or verbs... maybe an OperationType enum?
		#  -> options - such as: 
		# 		- marker file?  (create one or not - and where?)
		# 		- comment-removal options - See Enums.CommentRemovalOption.
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
	
		[tsmake.BuildResult]$buildResult = New-Object tsmake.BuildResult($BuildContext.BuildFile, ($BuildContext.Verb));  
	};
	
	process {
		# ====================================================================================================
		# 1. Create BuildManifest (which does a gob of work processing individual lines in the .build.sql file...)
		# ====================================================================================================	
		[tsmake.models.BuildFile]$buildFile = New-Object tsmake.models.BuildFile($BuildContext.BuildFile);
		
		# ====================================================================================================
		# 2. Check-for and report-on ParserErrors:
		# ====================================================================================================
		foreach ($fatalError in $buildFile.FatalParserErrors) {
			$buildResult.AddFatalError($fatalError);
		}
				
		foreach ($directive in $buildFile.Directives | Where-Object { $_.IsValid -eq $false	}) {
			$buildResult.AddFatalError((New-FatalParserError -Location ($directive.Location) -ErrorMessage ($directive.ValidationMessage)));
		}
		
		if ($buildResult.HasFatalError) {
			return;
		}
		
		# ====================================================================================================
		# 3. Evaluate + Process Global Directives (ROOT, OUTPUT, FILEMARKER, VERSIONCHECKER, etc.)
		# ====================================================================================================		
		if (Has-Value $buildFile.RootDirective) {
			$root = $buildFile.RootDirective;
			Write-Verbose "	RootPath Directive found. Path: [$($root.Path)] => PathType: $($root.PathType). Location: $($root.Location.LineNumber), $($root.Location.ColumnNumber).";
			
			$rootPath = $null;
			switch ($root.PathType) {
				"Absolute" {
					$rootPath = $root.Path;
					Write-Verbose "		Root Directive specifies Absolute root path: [$rootPath].";
				}
				"Relative" {
					$rootPath = Translate-Path -CurrentPath ($BuildContext.WorkingDirectory) -PathDirective ($root.Path);
					Write-Verbose "		Root Directive specifies Relative root path: [$rootPath].";
				}
				"Rooted" {
					$buildResult.AddFatalError((New-FatalParserError -Location ($root.Location) -ErrorMessage "Rooted Paths are NOT allowed for RootPath Directives."));
				}
				default {
					$msg = "Unknown PathType: [$($root.PathType)] specified for RootPath Directive.";
					$buildResult.AddFatalError((New-FatalParserError -Location ($root.Location) -ErrorMessage $msg));
				}
			}
			
			if (Has-Value $rootPath) {
				if (-not (Test-Path -Path ($rootPath))) {
					$buildResult.AddFatalError((New-FatalParserError -Location ($root.Location) -ErrorMessage "Directive Specified Root Path: [$rootPath] does NOT exist."));
				}				
				
				$BuildContext.SetRoot($rootPath);
				Write-Verbose "			Root Path Explicitly Set to: [$rootPath].";
			}
			
			if ($buildFile.HasFatalError) {
				return;
			}
		}
		
		# TODO: VERSION_CHECKER
		# TODO: FILEMARKER
		# TODO: anything else? 
		
		if ($null -ne $buildFile.OutputDirective) {
			$outputDirective = $buildFile.OutputDirective;
			Write-Verbose "	Output Directive found. Output Path: $($outputDirective.Path)";
			
			if (Is-Empty $BuildContext.Output) {
				Write-Verbose " 		-Output NOT specified via Command-Line. Attempting to set -Output from Output Directive on build file line # $($outputDirective.Location.LineNumber).";
				$outputPath = $null;
				switch ($outputDirective.PathType) {
					"Absolute" {
						$outputPath = = $outputDirective.Path;
						Write-Verbose "			Output Directive specifies Absolute path: [$($outputDirective.Path)].";
					}
					"Relative" {
						$outputPath = Translate-Path -CurrentPath $BuildContext.Root -PathDirective $outputDirective.Path;
						Write-Verbose "			Output Directive specifies Relative path: [$($outputDirective.Path)].";
					}
					"Rooted" {
						$unrooted = $outputDirective.Path.Replace("\\\", "");
						$outputPath = Translate-Path -CurrentPath $BuildContext.Root -PathDirective $unrooted;
						Write-Verbose "			Output Directive specifies Rooted path: [$($outputDirective.Path)].";
					}
					default {
						$msg = "Unknown PathType: [$($root.PathType)] specified for Output Directive.";
						$buildResult.AddFatalError((New-BuildError -Severity "Fatal" -ErrorMessage $msg));
					}
				}
				
				if (Has-Value $outputPath) {
					$outputDirectory = Split-Path -Path $outputPath -Parent;
					
					if (-not (Test-Path $outputDirectory)) {
						$buildResult.AddFatalError((New-BuildError -Severity "Fatal" -ErrorMessage "Output Directory Specified by Output Directive does NOT exist: [$outputDirectory]."));
					}
					
					if (Test-Path -Path $outputPath) {
						Write-Verbose "		Output file: [$outputPath] specified by Output Directory already exists.";
						
						# TODO: default behavior is to simply overwrite $outputPath. 
						# 		but, there can/will be some sort of OPTIONAL preference or switch that'll be the equivalent of -PreventOutputOverwrite
						# 		and, if when set to $true ... then: $buildResult.AddFatalError((New-RuntimeError -Severity "Fatal" -Location (xxx) -ErrorMessage "file exists - preference it so not overwrite. terminating..."));
					}
					
					$BuildContext.SetOutput($outputPath);
					Write-Verbose "				Output Explicitly Set to: [$outputPath].";
				}
			}
			else {
				Write-Verbose "		-Output Specified via Command Line supersedes ##OUTPUT directive set within .build file.";
			}
		}
		
		if (Is-Empty $BuildContext.Output) {
			$buildResult.AddFatalError((New-BuildError -Severity "Fatal" -ErrorMessage "Either specify -Output via command-line operations, or make sure to include an ##OUTPUT: directive within .build file."));
		}
		
		if ($buildResult.HasFatalError) {
			return;
		}
		
		# ====================================================================================================
		# 4. Validate Include File Paths...
		# ====================================================================================================			
		Write-Verbose "	Validating FILE and DIRECTORY paths.";
		foreach ($include in $buildFile.Directives | Where-Object {	$_.DirectiveName -in ("FILE", "DIRECTORY")	}) {
			
			$concretePath = $null;
			Write-Verbose "		Setting $($include.PathType) path for $($include.DirectiveName): $($include.Path).";
			switch ($include.PathType) {
				"Absolute" {
					$concretePath = $include.Path;
				}
				"Relative" {
					$concretePath = Translate-Path -CurrentPath ($BuildContext.WorkingDirectory) -PathDirective ($include.Path);
				}
				"Rooted" {
					$concretePath = Translate-Path -CurrentPath ($BuildContext.Root) -PathDirective ($include.Path.Replace("\\\", ""));
				}
			}
			Write-Verbose "			Concrete Path: $($concretePath).";
			
			try {
				$exists = Test-Path -Path $concretePath;
				if (-not ($exists)) {
					$buildResult.AddFatalError((New-BuildError -Severity Fatal -ErrorMessage "$($include.DirectiveName) not found: [$($concretePath)]." -Location ($include.Location)));
					Write-Verbose "				Concrete Path Not Found: [$concretePath]."
				}
			}
			catch {
				$buildResult.AddFatalError((New-BuildError -Severity Fatal -ErrorMessage "Path: [$concretePath] is invalid." -Exception $_));
			}
			
			$include.SetTranslatedPath($concretePath);
		}
		
		if ($buildResult.HasFatalError) {
			return;
		}
		
		# ====================================================================================================
		# 5. Process File Inclusions:
		# ====================================================================================================		
		Write-Verbose "	Processing FILE and DIRECTORY inclusions.";
		[tsmake.models.BuildManifest]$buildManifest = New-Object tsmake.models.BuildManifest];
		foreach ($line in $buildFile.Lines) {
			if (Has-Value $line.Directive) {
				switch ($line.Directive.DirectiveName) {
					{ $_ -in ("ROOT", "OUTPUT", "FILEMARKER", "VERSION_CHECKER", "COMMENT") } {
						continue; # skip
					}
					{ $_ -in ("FILE", "DIRECTORY") } {
						$include = [tsmake.models.IncludeFactory]::GetInclude($line.Directive);
						
						foreach ($fileToParse in $include.SourceFiles) {
							Write-Host "sourceFile to (Recursively) Include: $fileToParse";
							
							$fileLineage = New-Object tsmake.models.FileLineage(($BuildContext.BuildFile), $fileToParse);
							$processingResult = [tsmake.models.LineParser]::Instance.ParseLines($fileToParse, $fileLineage);
							
							$buildManifest.AddLines($processingResult.Lines);
							if ($processingResult.Errors.Count -gt 0) {
								$buildManifest.AddErrors($processingResult.Errors);
							}
							
							#TODO: Should I RETURN if there are any fatal errors? or keep going? 
						}
					}
					{ $_ -in ("CONDITIONAL_X", "CONDITIONAL_Y")	} {
						$buildManifest.AddLine($line);
					}
				}
			}
			else {
				$buildManifest.AddLine($line);
			}
		}
			
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
			
			
		
		#		foreach ($line in $buildFile.Lines) {
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
		# 	2. foreach LINE in $buildFile.Lines: 
		# 		a. if the line is a FILE-INCLUDE 
		# 		b or if the line is a DIRECTORY-INCLUDE 
		# 			then, RECURSIVELY, work through addition of any new directives. 
		# 			as in, for each file to be added (not file-include - but each FILE)... 
		# 			pull the contents into a new $fileBuffer or whatever. 
		# 					skim it's contents for INCLUDE (FILE/DIRECTORY) directives... 
		# 					and, if they're present ... include/replace them... over and over and over ... until we're done with 'INCLUDES'
		# 		c. if the line is a ROOT directive... then, skip/continue to the next line (i.e., don't copy into the next 'overall' buffer)
		# 			ditto on things like: ##OUTPUT, ##FILEMARKER and any other 'high-level'/meta-data directives.
		# 		d. if the line is NOT one of the 3x directives above, then ... just copy it out of $buildFile into $includeExplodedManifest. 
		
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
		
		foreach ($t in $buildFile.Tokens) {
			# For validation purposes ... need to go through each of these and: 
			#  a) see if it has a default or not. 
			#     if it does, check to see if I've got a TokenDefinition that matches and whether it PREVENTS defaults. 
			# 	  if it does not... see if I've got a TokenDefinition - and if it has a value. (If not, throw.)
			# 		and, actually: don't throw on error. instead, route into a helper func that stores 'parser errors' and ... if -ThrowOnError = $true .. then throw on first (or any) execution
			
			Write-Host "Token Location: $($t.Location.LineNumber), $($t.Location.ColumnNumber) -> TokenName: $($t.Name)  -> DefaultValue: $($t.DefaultValue)  ";
		}
		
		
#		Write-Host "-------------------------------------------------------";
#		Write-Host "Count: $($buildFile.Directives.Count)"
#		foreach ($d in $buildFile.Directives) {
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
		
		# TODO: Presumably, if we get 'here' then... there were no fatal errors or problems that stopped the build... 
		$buildResult.SetSucceeded();  
	};
	
	end {
		# TODO: copy these into a 'history/buffer' object so that callers (i.e., users) can interrogate
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