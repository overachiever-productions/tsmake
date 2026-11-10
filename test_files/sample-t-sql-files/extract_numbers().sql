/*

	SIGNATURE:

			DECLARE @sample_size sysname = N'1200 rows';
			DEClARE @intRowCount int = (SELECT CAST([output] as int) FROM dbo.extract_numbers(@sample_size));
			SELECT @intRowCount;



*/

USE [admindb];
GO

IF OBJECT_ID(N'dbo.extract_numbers', 'IF') IS NOT NULL
	DROP FUNCTION dbo.[extract_numbers];
GO

CREATE FUNCTION dbo.[extract_numbers] (@input nvarchar(MAX))
RETURNS table WITH SCHEMABINDING
RETURN 

	WITH filtered AS (
		SELECT
			[number] [position],
			SUBSTRING(@input, [number], 1) [char]
		FROM
			[dbo].[numbers]
		WHERE
			SUBSTRING(@input, [number], 1) LIKE N'[0-9]' AND [number] <= LEN(@input)
	)

	SELECT 
		STRING_AGG([char], N'') WITHIN GROUP (ORDER BY position) [output]
	FROM 
		filtered;;

GO