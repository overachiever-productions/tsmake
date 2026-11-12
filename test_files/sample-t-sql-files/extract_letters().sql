/*

	SIGNATURE:

			DECLARE @sample_size sysname = N'1200 rows';
			DEClARE @text sysname = (SELECT [output] FROM dbo.extract_letters(@sample_size));
			SELECT @text;



*/

USE [admindb];
GO

IF OBJECT_ID(N'dbo.[extract_letters]', 'IF') IS NOT NULL
	DROP FUNCTION dbo.[extract_letters];
GO

CREATE FUNCTION dbo.[extract_letters] (@input nvarchar(MAX))
RETURNS table WITH SCHEMABINDING
RETURN 

	WITH filtered AS (
		SELECT
			[number] [position],
			SUBSTRING(@input, [number], 1) [char]
		FROM
			[dbo].[numbers]
		WHERE
			SUBSTRING(@input, [number], 1) NOT LIKE N'[0-9]' AND [number] <= LEN(@input)
	)

	SELECT 
		STRING_AGG([char], N'') WITHIN GROUP (ORDER BY position) [output]
	FROM 
		filtered;;

GO