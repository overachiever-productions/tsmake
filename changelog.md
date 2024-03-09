# Change Log

## [0.2] - 2024-03-09 
Full rewrite/re-architect of solution implementation.  

### Changed 
- No longer attempting to use a native CLR (C#) only implementation. Switched to using PowerShell as core engine - with some functions - and underlying C# objects when/as needed. 

### Known Issues 
- This isn't even to proof of concept stage yet - i.e., doesn't work, BUT the main workflows are STARTING to shape up - i.e., there is a base function for `Invoke-TsmBuild`, some helpers (for tokens), and the initial implementation of a pipeline. 

v0.3 will start to resemble a proof of concept. 