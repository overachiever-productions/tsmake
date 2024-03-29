Set-StrictMode -Version 1.0;

<#


	Import-Module -Name "D:\Dropbox\Repositories\tsmake" -Force;

	Set-Location "D:\Dropbox\Repositories\tsmake\~~spelunking";
$global:VerbosePreference = "Continue";
	Invoke-TsmBuild -BuildFile "D:\Dropbox\Repositories\tsmake\~~spelunking\simplified.build.sql" -Tokens "Doc_Link:https://www.overachiever.net", "VERSION:11.2";


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
		[tsmake.models.BuildManifest]$buildManifest = New-Object tsmake.models.BuildManifest($buildFile);
		
		foreach ($line in $buildFile.Lines) {
			if (Has-Value $line.Directive) {
				switch ($line.Directive.DirectiveName) {
					{ $_ -in ("ROOT", "OUTPUT", "FILEMARKER", "VERSION_CHECKER", "COMMENT") } {
						continue; # skip - i.e., don't copy into buildManifest
					}
					{ $_ -in ("FILE", "DIRECTORY") } {
						$include = [tsmake.models.IncludeFactory]::GetInclude($line.Directive, $fileManager, $BuildContext.WorkingDirectory, $BuildContext.Root);
						
						if ($include.Errors.Count -gt 0) {
							$buildResult.AddErrors($include.Errors);
						}
						else {
							foreach ($fileToParse in $include.SourceFiles) {
								$processingResult = [tsmake.models.FileProcessor]::ProcessFileLines($line, $fileToParse, "IncludedFile", $fileManager, $BuildContext.WorkingDirectory, $BuildContext.Root);
								
								if ($processingResult.Errors.Count -gt 0) {
									$buildResult.AddErrors($processingResult.Errors);
								}
								
								$buildManifest.AddLines($processingResult);
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
		
		if ($buildResult.HasErrors) {
			return;
		}
		
		# ====================================================================================================
		# 4: Validate Tokens:
		# ====================================================================================================		
		Write-Verbose "	Validating Tokens...";
		foreach ($token in $buildManifest.Tokens) {
			
			# Poor man's formatting: 
#			Write-Host "Token Name: $($tokenName)"
#			Write-Host "	Has Default: ($((Has-Value $token.DefaultValue))) => $($token.DefaultValue)"
#			Write-Host "  DEFINITION: "
#			Write-Host "	Definition: ($($tokenDefinition.Name)) -> DefaultBuildValue: ($($tokenDefinition.DefaultBuildValue))  -> AllowInlineDefaults: ($($tokenDefinition.AllowInlineDefaults)) -> AllowBlanks: ($($tokenDefinition.AllowBlanks))"
			
			[string]$tokenName = $token.Name;
			$tokenDefinition = $global:tsmTokenRegistry.GetTokenDefinition($tokenName);
			
			if (Has-Value $token.DefaultValue) {
				if (Has-Value $tokenDefinition) {
					if ($false -eq $tokenDefinition.AllowInlineDefaults) {
						Write-Verbose "		Token $($token.Name) has a default value of $($token.DefaultValue) and token DEFINITION prevents default values.";
						$buildResult.AddError((New-ParserError -ErrorMessage "Token $($token.Name) has a default value of $($token.DefaultValue) and token DEFINITION prevents default values." -Location ($token.Location) ));
					}
				}
			}
			else {
				if (Has-Value $tokenDefinition) {
					if ((Has-Value $tokenDefinition.DefaultBuildValue) -or (Has-Value $tokenDefinition.SpecifiedBuildValue)) {
						Write-Verbose "			Token: [$($token.Name)] has a Specified or Default-Build Value Specified.";
					}
					else {
						if ($tokenDefinition.AllowBlanks) {
							Write-Verbose "			Token: [$($token.Name)] has a definition that allows BLANK values.";
						}
						else {
							Write-Verbose "			Token: [$($token.Name)] has no inline default/value, no specified value via Input or Config, and does NOT allow blank values.";
							$buildResult.AddError((New-ParserError -ErrorMessage "Token: [$($token.Name)] has no inline default/value, no specified value via Input or Config, and does NOT allow blank values." -Location ($token.Location)));
						}
					}
				}
				else {
					Write-Verbose "		Undefined Token: $($token.Name) at <location goes here>."; # tokens can't be undefined, right?
					$buildResult.AddError((New-ParserError -ErrorMessage "Undefined Token: [$($token.Name)]." -Location ($token.Location)));
				}
			}
		}
		
		if ($buildResult.HasErrors) {
			return;
		}
		
		# ====================================================================================================
		# 5: Conditional Processing:
		# ====================================================================================================			
		# I can skip this for initial prototypes... but, here is where I should be handling stuff like that - in terms of workflow. 
		#  	and... i think I really just want to validate that everything is in place here (at this step) and handle the actual outputs? down below in step #7, right?
		
		
		# ====================================================================================================
		# 6: Comment Extraction / Inline Documentation Processing:
		# ====================================================================================================	
		# depending upon -Verb and other settings/etc. ... extract any .DOCUMENTATION and ... write it out to wherever it should be going - along with, eventually, formatters and the likes. 
		
		
		# ====================================================================================================
		# 7: Output + Comment Removal Processing: 
		# ====================================================================================================	
		# NOTE: here's where I also need to replace {{##TOKENS}}
		# basically, stream what's 'left' to disk - as a target/output/artifact file - stripping and removing comments along the way if/as directed by comment-removal preferences. 
		
		# ====================================================================================================
		# 8. Optional File Marker
		# ====================================================================================================	
		
		# Dump a file marker if/as directed. 
		return;
#		foreach ($token in $buildManifest.Tokens) {
#			Write-Host "TOKEN: $($token.Name) => Default:[$($token.DefaultValue)] ";
#			Write-Host "	At: $($token.Location.FileName)($($token.Location.LineNumber),$($token.Location.ColumnNumber)) ";
#			Write-Host "";
#		}
#		
#		Write-Host "";
#		Write-Host "--------------------------------------------------------------------------";
#		
		# TODO: there's something effed-up with directives... they're just not getting into this thing like they should be. 
		foreach ($directive in $buildManifest.Directives) {
			Write-Host "DIRECTIVE: $($directive.DirectiveName)";
			Write-Host "	At: $($directive.Location.FileName)($($directive.Location.LineNumber),$($directive.Location.ColumnNumber))"
		}
		
		Write-Host "";
		Write-Host "--------------------------------------------------------------------------";
		
		# TODO: WTH??? there's a problem with $codeString.GetLocation() ... which boggles the mind cuz... CodeStrings and CodeComments both derive from BaseLineDecorator and ... .GetLocation() works for Comments. 
		# 		ah... maybe I'm not setting locations for CodeStrings? (if .Location was empty (0 entries), then... "" would make sense for output.)
		# 		nope. being set - cuz unit tests PROVE this info is there... 
		foreach ($codeString in $buildManifest.CodeStrings) {
			# poor man's string formatting: 
			Write-Host "STRING: $($codeString.Text.Replace("`r`n", "[CR][LF]"))";
			Write-Host "	At: $($codeString.GetLocation())";
			Write-Host "";
		}
		
		Write-Host "";
		Write-Host "--------------------------------------------------------------------------";
		
		foreach ($comment in $buildManifest.Comments) {
			# poor man's comment formatting:
			Write-Host "COMMENT: $($comment.Text.Replace("`r`n", "[CR][LF]"))";
			Write-Host "	At: $($comment.GetLocation())";
			Write-Host "";
		}
		
		Write-Host "";
		Write-Host "--------------------------------------------------------------------------";
		
		foreach ($line in $buildManifest.Lines) {
			
			$writeLine = $false;
			
			if ($line.HasComment -and $line.HasString) {
				$writeLine = $true;
			}
			
			# Poor Man's Summary/Detail of each line:
			if ($writeLine) {
				Write-Host "$($line.LineNumber) => $($line.RawContent)";
				Write-Host "	HasStrings? ($($line.HasString)) HasComments? ($($line.HasComment)) CommentType ($($line.CommentType))";
				Write-Host "		IsHeaderComment? ($($line.IsHeaderComment))  IsStringNotComment? ($($line.IsStringNotComment))";
				#Write-Host "		Comment[0].Text => $($line.CodeComments[0].Text)"
				$ii = 0;
				foreach ($comment in $line.CodeComments) {
					Write-Host "			-> Comment[$($ii)]: $($comment.Text)"
					$ii = $ii + 1;
				}
				Write-Host "	$($line.GetLocation())";
				Write-Host "";
			}
		}
		
		# ====================================================================================================
		# 9. Finalization + Metrics/Reporting on build process, etc.:
		# ====================================================================================================			
		
		
		# TODO: Presumably, if we get 'here' then... there were no errors or problems that stopped the build... 
		$buildResult.SetSucceeded();  
	};
	
	end {
		
		foreach ($e in $buildResult.Errors) {
			Write-Host "Error: $($e.ErrorMessage)";
		}
		
		
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