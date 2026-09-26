Set-StrictMode -Version 3.0;


# set/define a single (extant) token's VALUE or ... options (.AllowEmpty)
# 		this can't be used to add a new token... 
function Set-TsmToken {
	
}

function Add-TsmToken {
	
}

function New-TsmToken {
	# single token ... 
	
}

function New-TsmTokensCollection {
	# collection of tokens
}

# serialized string or a hashtable ? are the only inputs here. 
# 	 note that a CONFIG file can/will have the option for defined tokens. but we won't import from a config file. 
# 			instead IF a "Config Object" has tokens, those can be ADDED to build/etc by IMPORTING those tokens.
# 			so ... yeah, we need an option for a COLLECTION of tokens as well. 
# 		basically: Import-TsmTokens allows a number of different ways to add 1 or more tokens into the tsmTokenRegistry... 
function Import-TsmTokens {
	[CmdletBinding()]
	[Alias("Import-Tokens")]
	param (
		
	);
	
	begin {
		
	};
	
	process {
		
	};
	
	end {
		
	};
}



# REFACTOR: to ... Initialize-TsmTokens or ... Unregister-TsmTokens... 
filter Remove-TsmTokens {
	
}

filter Import-TsmBaseFunctionalityTokens {
	
	# add in all of the core/default tokens like ... 
	#    copyright... doclink, project_link... migration_id, version... 
	# 		
}

# ... might be SET vs ADD ... i.e., this filter might be named Set-SystemToken... (user-supplied values ... but against SYSTEM-LEVEL (built-in) tokens vs custom-tokens)
filter Add-BuildParameterTokens {
	# pass in ... VERSION and SUMMARY 'tokens'
	# 		set their source to ... SYSTEM vs USER... 
	
}

filter Get-TsmToken {
	
}
