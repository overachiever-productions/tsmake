
internal static class FAKE_BUILD_SCRIPTS
{
	internal static class S4_SAMPLE
	{
		internal const string FILE_START = @"
--##OUTPUT: \\Deployment
--##NOTE: This is a build file only (i.e., it stores upgade/install directives + place-holders for code to drop into admindb, etc.)
/*

	REFERENCE:
		- License, documentation, and source code at: 
			https://github.com/overachiever-productions/s4/

	NOTES:
		- This script will either install/deploy S4 version ##{{S4version}} or upgrade a PREVIOUSLY deployed version of S4 to ##{{S4version}}.
		- This script will create a new, admindb, if one is not already present on the server where this code is being run.

	Deployment Steps/Overview: 
		1. Create admindb if not already present.
		2. Create core S4 tables (and/or ALTER as needed + import data from any previous versions as needed). 
		3. Cleanup any code/objects from previous versions of S4 installed and no longer needed. 
		4. Deploy S4 version ##{{S4version}} code to admindb (overwriting any previous versions). 
		5. Report on current + any previous versions of S4 installed. 

*/

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 1. Create admindb if/as needed: 
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
SET NOCOUNT ON;

USE[master];
GO

IF NOT EXISTS(SELECT NULL FROM master.sys.databases WHERE [name] = 'admindb') BEGIN
   CREATE DATABASE[admindb];  -- TODO: look at potentially defining growth size details - based upon what is going on with model/etc.

  ALTER AUTHORIZATION ON DATABASE::[admindb] TO sa;

		ALTER DATABASE[admindb] SET RECOVERY SIMPLE;  -- i.e., treat like master/etc.
   END;
GO

--##NOTE: This is another note... it, too, should be captured and ... replaced.
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 2. Core Tables:
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

USE[admindb];
GO

IF OBJECT_ID('version_history', 'U') IS NULL BEGIN

	CREATE TABLE dbo.version_history(
		version_id int IDENTITY(1,1) NOT NULL,
		version_number varchar(20) NOT NULL,
		[description] nvarchar(200) NULL, 
		deployed datetime NOT NULL CONSTRAINT DF_version_info_deployed DEFAULT GETDATE(), 
		CONSTRAINT PK_version_info PRIMARY KEY CLUSTERED(version_id)
	);

	EXEC sys.sp_addextendedproperty
		@name = 'S4',
		@value = 'TRUE',
		@level0type = 'Schema',
		@level0name = 'dbo',
		@level1type = 'Table',
		@level1name = 'version_history';
		END;

DECLARE @CurrentVersion varchar(20) = N'##{{S4version}}';

-- Add previous details if any are present: 
DECLARE @version sysname; 
DECLARE @objectId int;
DECLARE @createDate datetime;
SELECT @objectId = [object_id], @createDate = create_date FROM master.sys.objects WHERE[name] = N'dba_DatabaseBackups_Log';
		SELECT @version = CAST([value] AS sysname) FROM master.sys.extended_properties WHERE major_id = @objectId AND[name] = 'Version';

		IF NULLIF(@version,'') IS NOT NULL BEGIN
	IF NOT EXISTS(SELECT NULL FROM dbo.version_history WHERE [version_number] = @version) BEGIN
	   INSERT INTO dbo.version_history(version_number, [description], deployed)
		VALUES(@version, N'Found during deployment of ' + @CurrentVersion + N'.', @createDate);
		END;
END;
GO

-----------------------------------
--##INCLUDE: Common\tables\backup_log.sql

-----------------------------------
--##INCLUDE: Common\tables\restore_log.sql";
	}

	internal static class MISC
	{
		internal const string MULTI_LINE_WITH_NOTE = @"DECLARE @version sysname; 
DECLARE @objectId int;
DECLARE @createDate datetime;

--##NOTE: This is another note... it, too, should be captured and ... replaced.

USE [master];
GO
";
	}
}