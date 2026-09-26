Set-StrictMode -Version 3.0;


# returns a new, blank [pscustomobject] with ... slots for config-values ... 
# 		probably ... also, has 'methods' for adding/setting various properties... 
# 		i.e. this is what someone would use to programatically create a config for use during build, doc, gen, etc. 
function New-TsmConfiguration {
	[CmdletBinding()]
	param (
	);
	
	begin {
		
	};
	
	process {
		
	};
	
	end {
		
	};
}

# reads from file ... returns a pscustomObject that's basically a hash-table ... ish. 
#    CONFIG files can/will provide 5x config areas: COMMON, BUILD, DOC, GENERATE, RUN... 
# 		each of these 'areas' can/will be pulled out by various 'verbs' (BUILD-xxx, DOC-xxx) as needed. 
# 		and ... sigh, COMMON values are fine. but will be superseded by SPECIFICs whenever found. 
function Import-TsmConfiguration {
	[CmdletBinding()]
	param (
	);
	
	begin {
		
	};
	
	process {
		
	};
	
	end {
		
	};
}