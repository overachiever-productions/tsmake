@{
	RootModule			   = 'tsmake.psm1'
	ModuleVersion		   = '0.0.5'
	GUID				   = '9a6f967a-1f27-48d4-8838-802ff3bfad98'
	Author				   = 'Michael K. Campbell'
	CompanyName		       = 'OverAchiever Productions, LLC.'
	Copyright			   = '(c) 2025. All rights reserved.'
	Description		       = 'Module description'
	
	# Supported PSEditions
	# CompatiblePSEditions = @('Core', 'Desktop')
	
	PowerShellVersion	   = '7.2'
	DotNetFrameworkVersion = '8.0.0'
	ProcessorArchitecture  = 'None'
	RequiredModules	       = @()
	RequiredAssemblies	   = @()
	ScriptsToProcess	   = @()
	TypesToProcess		   = @()
	FormatsToProcess	   = @()
	NestedModules		   = @()
	FunctionsToExport	   = @('Import-Types') #For performance, list functions explicitly
	CmdletsToExport	       = '*'
	VariablesToExport	   = '*'
	AliasesToExport	       = '*' #For performance, list alias explicitly
	ModuleList			   = @()
	FileList			   = @()
	PrivateData		       = @{
		PSData = @{
			# Tags = @()
			# LicenseUri = ''
			# ProjectUri = ''
			# IconUri = ''
			# ReleaseNotes = ''
		}
	}
}