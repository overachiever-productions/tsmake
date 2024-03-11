Set-StrictMode -Version 1.0;

<#

	Set-Location "D:\Dropbox\Repositories\tsmake\~~spelunking";

	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;
$global:VerbosePreference = "Continue";
	Invoke-TsmBuild -Tokens "Doc_Link:https://www.overachiever.net";


#>


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
		
		# TODO: move this into the BuildContext (along with any other 'build intrinsics')
		[tsmake.models.BaseFileManager]$fileManager = New-Object tsmake.models.BaseFileManager;
	};
	
	process {
		# ====================================================================================================
		# 1. Create / Parse Build File for Errors and any key/needed Directives:
		# ====================================================================================================	
		[tsmake.models.BuildFile]$buildFile = New-Object tsmake.models.BuildFile($BuildContext.BuildFile, $fileManager);
		
		foreach ($error in $buildFile.Errors) {
			$buildResult.AddError($error);
		}
		
		if ($buildResult.HasErrors) {
			return;
		}
		
		# ====================================================================================================
		# 2. Evaluate + Process Global Directives (ROOT, OUTPUT, FILEMARKER, VERSIONCHECKER, etc.)
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
					$rootPath = $fileManager.TranslatePath($root.Path, $root.PathType, $BuildContext.WorkingDirectory, $BuildContext.Root);
					Write-Verbose "		Root Directive specifies Relative root path: [$rootPath].";
				}
				"Rooted" {
					$buildResult.AddError((New-ParserError -Location ($root.Location) -ErrorMessage "Rooted Paths are NOT allowed for RootPath Directives."));
				}
				default {
					$msg = "Unknown PathType: [$($root.PathType)] specified for RootPath Directive.";
					$buildResult.AddError((New-ParserError -Location ($root.Location) -ErrorMessage $msg));
				}
			}
			
			if (Has-Value $rootPath) {
				if (-not (Test-Path -Path ($rootPath))) {
					$buildResult.AddError((New-ParserError -Location ($root.Location) -ErrorMessage "Directive Specified Root Path: [$rootPath] does NOT exist."));
				}				
				
				$BuildContext.SetRoot($rootPath);
				Write-Verbose "			Root Path Explicitly Set to: [$rootPath].";
			}
			
			if ($buildFile.HasErrors) {
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
						$outputPath = $fileManager.TranslatePath($outputDirective.Path, $outputDirective.PathType, $BuildContext.WorkingDirectory, $BuildContext.Root);
						Write-Verbose "			Output Directive specifies Relative path: [$($outputDirective.Path)].";
					}
					"Rooted" {
						$unrooted = $outputDirective.Path.Replace("\\\", "");
						$outputPath = $fileManager.TranslatePath($unrooted, $outputDirective.PathType, $BuildContext.WorkingDirectory, $BuildContext.Root);
						Write-Verbose "			Output Directive specifies Rooted path: [$($outputDirective.Path)].";
					}
					default {
						$msg = "Unknown PathType: [$($root.PathType)] specified for Output Directive.";
						$buildResult.AddError((New-BuildError -ErrorMessage $msg));
					}
				}
				
				if (Has-Value $outputPath) {
					$outputDirectory = Split-Path -Path $outputPath -Parent;
					
					if (-not (Test-Path $outputDirectory)) {
						$buildResult.AddError((New-BuildError -ErrorMessage "Output Directory Specified by Output Directive does NOT exist: [$outputDirectory]."));
					}
					
					if (Test-Path -Path $outputPath) {
						Write-Verbose "		Output file: [$outputPath] specified by Output Directory already exists.";
						
						# TODO: default behavior is to simply overwrite $outputPath. 
						# 		but, there can/will be some sort of OPTIONAL preference or switch that'll be the equivalent of -PreventOutputOverwrite
						# 		and, if when set to $true ... then: $buildResult.AddError((New-RuntimeError -Location (xxx) -ErrorMessage "file exists - preference it so not overwrite. terminating..."));
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
			$buildResult.AddError((New-BuildError -ErrorMessage "Either specify -Output via command-line operations, or make sure to include an ##OUTPUT: directive within .build file."));
		}
		
		if ($buildResult.HasErrors) {
			return;
		}

		# ====================================================================================================
		# 3. Process File Inclusions:
		# ====================================================================================================		
		Write-Verbose "	Processing FILE and DIRECTORY inclusions.";
		[tsmake.models.BuildManifest]$buildManifest = New-Object tsmake.models.BuildManifest];
		
		foreach ($line in $buildFile.Lines) {
			if (Has-Value $line.Directive) {
				switch ($line.Directive.DirectiveName) {
					{ $_ -in ("ROOT", "OUTPUT", "FILEMARKER", "VERSION_CHECKER", "COMMENT") } {
						continue; # skip - i.e., don't copy into buildManifest
					}
					{ $_ -in ("FILE", "DIRECTORY") } {
	#Write-Host "$($line.Directive.DirectiveName) => Path: $($line.Directive.Path)"
						$include = [tsmake.models.IncludeFactory]::GetInclude($line.Directive, $fileManager, $BuildContext.WorkingDirectory, $BuildContext.Root);
					
						foreach ($fileToParse in $include.SourceFiles) {
	#Write-Host "	For Directive: $($line.Directive.DirectiveName) (Path: [$($line.Directive.Path)]) => SourceFile to (Recursively) Include: $fileToParse";
							
							$processingResult = [tsmake.models.LineProcessor]::TransformLines($fileToParse, "IncludedFile", $fileManager, $BuildContext.WorkingDirectory, $BuildContext.Root);
							$buildManifest.AddLines($processingResult.Lines);
							
							if ($processingResult.Errors.Count -gt 0) {
								$buildManifest.AddErrors($processingResult.Errors);
							}
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
		
		if ($buildManifest.Errors.Count -gt 0) {
			$buildResult.AddErrors($buildManifest.Errors);
		}
		
		if ($buildResult.HasErrors) {
			return;
		}
		
		foreach ($line in $buildManifest.Lines) {
			#Write-Host "LINE: $($line.RawContent)"
			
#			if ($line.IsComment) {
#				Write-Host "$($line.LineNumber): $($line.CommentText)";
#			}
			
			if ($line.IsBlockComment) {
				Write-Host "$($line.LineNumber): $($line.CommentText)";
			}
			
			
			
#			if ($line.IsComment -and -not($line.IsCommentOnly)) {
#				#Write-Host "Comment: $($line.Content)"
#				Write-Host "CommentFree: $($line.CommentFreeContent)";
#			}
#			if ($line.IsBlockComment) {
#				Write-Host "BlockComment: $($line.Content)";
#			}
#			if ($line.HasMultipleBlockComments) {
#				Write-Host "Multi-block: $($line.Content)";
#			}
#			if ($line.IsBlockCommentStart) {
#				Write-Host "Block-Start: $($line.Content) -> $($line.LineNumber) ($($line.Source))";
#			}
#			if ($line.HasMultipleBlockComments -and $line.IsBlockCommentStart) {
#				Write-Host "Multi-Block-Start: $($line.Content)";
#			}
		}
		
		
		# at this point, I've got ALL lines EXCLUDING: root, output, and other global directives AND any removed/stripped comments are gone as well. 
		# 		which means I can/should review ALL tokens and throw errors on any non-valid tokens. 
		# 			i.e., I WAS doing this previously against lines in the $buildManifest... but that's too early. Simply 'move' that logic 'down here'
		# 		otherwise, once those are CHECKED/VALIDATED... 
		# 		then it's time to create an $outputFileBuilder/Buffer 
		# 			a) go through and grab-out/replace/process any CONDITIONAL lines/etc. as needed.
		# 			b) then, for each line in $buildManifest:
		# 					if(line.hasTOken) - do the replacement and then copy to $outputFileBuilder/Buffer. 
		# 					else ... just copy into the $outputFileBuilder. 
		
		# then ... work on docs and/or marker files. 
		# 	and, finally, serialize everything where it needs to go. 
		
		
		
		
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
		
		
		
#		Write-Host "-------------------------------------------------------";
#		
#		foreach ($t in $buildFile.Tokens) {
#			# For validation purposes ... need to go through each of these and: 
#			#  a) see if it has a default or not. 
#			#     if it does, check to see if I've got a TokenDefinition that matches and whether it PREVENTS defaults. 
#			# 	  if it does not... see if I've got a TokenDefinition - and if it has a value. (If not, throw.)
#			# 		and, actually: don't throw on error. instead, route into a helper func that stores 'parser errors' and ... if -ThrowOnError = $true .. then throw on first (or any) execution
#			
#			Write-Host "Token Location: $($t.Location.LineNumber), $($t.Location.ColumnNumber) -> TokenName: $($t.Name)  -> DefaultValue: $($t.DefaultValue)  ";
#		}
		
		# TODO: Presumably, if we get 'here' then... there were no errors or problems that stopped the build... 
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