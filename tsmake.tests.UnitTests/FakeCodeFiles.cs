using System.Diagnostics.CodeAnalysis;

namespace tsmake.Tests.UnitTests
{
	[ExcludeFromCodeCoverage]
	internal static class FAKE_CODE_FILES
	{

		internal const string UDF_WITH_HEADER_COMMENTS = @"/*

	EXAMPLES: 
		'EXEC' style: 
			DECLARE @isRunning bit; 
			EXEC @isRunning = admindb.dbo.is_job_running 'Fake Job';
			SELECT @isRunning;


		SELECT @param style: 
			DECLARE @isRunning bit;
			SELECT @isRunning = admindb.dbo.is_job_running('Fake Job');
			SELECT @isRunning;

		SELECT style: 
			SELECT admindb.dbo.is_job_running('Fake Job');

*/


USE [admindb];
GO

IF OBJECT_ID('dbo.is_job_running','FN') IS NOT NULL
	DROP FUNCTION dbo.is_job_running;
GO

CREATE FUNCTION dbo.is_job_running (@JobName sysname) 
RETURNS bit 
	WITH RETURNS NULL ON NULL INPUT
AS 

	-- {copyright}

	BEGIN;
		
		DECLARE @output bit = 0;

		IF EXISTS (
			SELECT 
				NULL
			FROM 
				msdb.dbo.sysjobs j 
				INNER JOIN msdb.dbo.sysjobactivity ja ON [j].[job_id] = [ja].[job_id] 
			WHERE 
				ja.[session_id] = (SELECT TOP (1) session_id FROM msdb.dbo.[syssessions] ORDER BY [agent_start_date] DESC)
				AND [ja].[start_execution_date] IS NOT NULL 
				AND [ja].[stop_execution_date] IS NULL -- i.e., still running
				AND j.[name] = @JobName
		)  
		  BEGIN 
			SET @output = 1;
		END;

		RETURN @output;

	END; 
GO";

		internal const string UDF_WITH_HEADER_COMMENTS_AND_NESTED_COMMENTS = @"/*

	EXAMPLES: 
		'EXEC' style: 
			DECLARE @isRunning bit; 
			EXEC @isRunning = admindb.dbo.is_job_running 'Fake Job';
			SELECT @isRunning;


		SELECT @param style: 
			DECLARE @isRunning bit;
			SELECT @isRunning = admindb.dbo.is_job_running('Fake Job');
			SELECT @isRunning;

		SELECT style: 
			SELECT admindb.dbo.is_job_running('Fake Job');

*/


USE [admindb];
GO

IF OBJECT_ID('dbo.is_job_running','FN') IS NOT NULL
	DROP FUNCTION dbo.is_job_running;
GO

CREATE FUNCTION dbo.is_job_running (@JobName sysname) 
RETURNS bit 
	WITH RETURNS NULL ON NULL INPUT
AS 

	-- {copyright}

	BEGIN;
		
		DECLARE @output bit = 0;
		
		/*
			These are some nested comments. They're not needed. They're just here to 
				test that HeadingCommentRemovalOperator won't remove them... 
		*/

		IF EXISTS (
			SELECT 
				NULL
			FROM 
				msdb.dbo.sysjobs j 
				INNER JOIN msdb.dbo.sysjobactivity ja ON [j].[job_id] = [ja].[job_id] 
			WHERE 
				ja.[session_id] = (SELECT TOP (1) session_id FROM msdb.dbo.[syssessions] ORDER BY [agent_start_date] DESC)
				AND [ja].[start_execution_date] IS NOT NULL 
				AND [ja].[stop_execution_date] IS NULL -- i.e., still running
				AND j.[name] = @JobName
		)  
		  BEGIN 
			SET @output = 1;
		END;

		RETURN @output;

	END; 
GO";


		internal const string UDF_WITHOUT_HEADER_BUT_WITH_MULTI_LINE_COMMENT = @"
USE [admindb];
GO

IF OBJECT_ID('dbo.is_job_running','FN') IS NOT NULL
	DROP FUNCTION dbo.is_job_running;
GO

CREATE FUNCTION dbo.is_job_running (@JobName sysname) 
RETURNS bit 
	WITH RETURNS NULL ON NULL INPUT
AS 

	-- {copyright}

	BEGIN;
		
		DECLARE @output bit = 0;
		
		/*
			nested comment here... 
		*/

		IF EXISTS (
			SELECT 
				NULL
			FROM 
				msdb.dbo.sysjobs j 
				INNER JOIN msdb.dbo.sysjobactivity ja ON [j].[job_id] = [ja].[job_id] 
			WHERE 
				ja.[session_id] = (SELECT TOP (1) session_id FROM msdb.dbo.[syssessions] ORDER BY [agent_start_date] DESC)
				AND [ja].[start_execution_date] IS NOT NULL 
				AND [ja].[stop_execution_date] IS NULL -- i.e., still running
				AND j.[name] = @JobName
		)  
		  BEGIN 
			SET @output = 1;
		END;

		RETURN @output;

	END; 
GO";

	}
}