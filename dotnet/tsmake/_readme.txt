Notes about this .slnx:
- Using Visual Studio 2026
- Project(s) target .netcore 8.0 (yeah yeah, it's old, but trying to hit a 'base' feature-set in terms of compilation (C# syntax). 
- Using System.Management.Automation 7.4.13 (which works with .netcore8.0)

AND... none of the above 'matters' - other than IF you want to try and open this solution yourself. 

i.e., by including 'raw' .cs files in tsmake:
- PowerShell will COMPILE these to .netcore XXX upon Install-Module (where XXX is whatever version is installed with Powershell). 
- i.e., .cs files are compiled locally - which avoids the need to create .dlls for various different versions of .netcore that MIGHT be in place. 

IMPORTANT:
- Because .cs files are compiled on-box / runtime / install-module-time ... the global using directives in Globals.cs are WICKED important. 
	e.g., Visual Studio will show that MANY of them are NOT needed - and they're NOT if you're compiling via later versions of VS. 
		BUT... PowerShell uses a different compiler - and said directives ARE required. 