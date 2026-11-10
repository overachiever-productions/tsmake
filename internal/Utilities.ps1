Set-StrictMode -Version 3.0;

# ====================================================================================================
# Intrinsics:
# ====================================================================================================	
$tsmTokenRegistry = [tsmake.data_models.TokenDefinitionRegistry]::Instance;


# ====================================================================================================
# Helpers / Utilities:
# ====================================================================================================	
filter Remove-Comments {
	param (
		[ValidateSet('RemoveHeader', 'RemoveDoc', 'RemoveEol', 'RemoveBlock', 'RemoveAll', 'None')]
		[string[]]$CommentRemovalDirectives = @('None')
	)
	
	$map = @{
		None = [CommentRemovalDirectives]::None
		RemoveHeader = [CommentRemovalDirectives]::RemoveHeaderComments
		RemoveDoc = [CommentRemovalDirectives]::RemoveDocComments
		RemoveEol = [CommentRemovalDirectives]::RemoveLineEndComments
		RemoveBlock = [CommentRemovalDirectives]::RemoveBlockComments
		RemoveAll = [CommentRemovalDirectives]::RemoveAllComments
	}
	
	$flags = [CommentRemovalDirectives]::None
	foreach ($d in $CommentDirectives) {
		$flags = $flags -bor $map[$d]
	}
	
	return $flags;
}



# Errors will use Verb + Phase. for context. (Hmm. except verb doesn't cover things like initialization / prep / etc. So, maybe Phase + Step?)