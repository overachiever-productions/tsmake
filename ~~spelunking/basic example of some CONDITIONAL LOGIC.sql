/*

SEE COMMENTS at bottom of file - about conditional blocks implementation options... 

			DECLARE @x nvarchar(MAX);
			EXEC [admindb].dbo.[test_sproc_with_string_aggregation] 
				@Parmeter1 = 'NOT NULL', 
				@Output = @x OUTPUT;

			SELECT @x;


*/

USE [admindb];
GO

IF OBJECT_ID('dbo.test_sproc_with_string_aggregation','P') IS NOT NULL
	DROP PROC dbo.[test_sproc_with_string_aggregation];
GO

CREATE PROC dbo.[test_sproc_with_string_aggregation]
	@Parmeter1			sysname, 
	@Output				nvarchar(MAX)		OUTPUT
AS
    SET NOCOUNT ON; 

	-- {{##COPYRIGHT}}
	-- {{##VERSION}}
	
	SET @Parmeter1 = NULLIF(@Parmeter1, '');

	IF @Parmeter1 IS NULL BEGIN 
		RAISERROR('Meh. @Parameter1 CAN NOT be NULL...', 16, 1);
		RETURN -10;
	END;
	
-- ## CONDITIONAL_BASE ## Downlevel string-aggregation... 
	WITH aggregated AS ( 
		SELECT
			STUFF(
			(SELECT N'', ' ,' + [name] FROM master.sys.databases ORDER BY [database_id] FOR XML PATH(''), TYPE).value(N'.[1]', N'nvarchar(max)'), 
			1, 2, N'') aggregated_data
	)
-- ## CONDITIONAL_VERSION(> 14.0.5)		
	WITH aggregated AS ( 
		SELECT 
			STRING_AGG([name], N', ') WITHIN GROUP (ORDER BY [database_id]) aggregated_data
		FROM 
			master.sys.[databases]
	) 
-- ## CONDITIONAL_END

	SELECT 
		@Output = aggregated_data 
	FROM 
		aggregated; 


	RETURN 0;
GO

/*
	So... there are a couple of ways that I can implement the above. 
	But, in each case, need to remember that I won't know what VERSION we're dealing with until runtime - i.e., need to have a FULLY T-SQL implementation here... 

	A. ALTER with a REPLACE operation - inside T-SQL. 
		Something like: 

				CREATE / ALTER <name_of_module_here>; -- i.e., this is the CONDITIONAL_BASE definition
				GO

				IF dbo.engine_version > x BEGIN 
					DECLARE @def nvarchar(max) = (SELECT definition FROM sys.sql_modules WHERE name = '<name_of_module_here>');
					SET @def = REPLACE(@def, N'this is a string here that represents the EXACT text of the CONDITIONAL_BASE clause', N'this is a string that represents the replacement text for version N');
					EXEC sp_executesql 
						@def -- which'll be an ALTER script... (er, well, I'd need to make it into one). 
				END;

		I'm NOT too wild about the above... just because I don't think that T-SQL is the best place to do this kind of replacement. 
			
	B. Do the identification and replacing inside of C# / tsmake itself and ... have (a) different version(s) for each condition. 
			CREATE / ALTER <name_of_module_here>; -- i.e., this is the CONDITIONAL_BASE definition
			GO

			IF dbo.engine_version > xx BEGIN 
				DECLARE @conditionalVersionXXX nvarchar(MAX) = N'full blown copy of the module - but with <CONDITIONAL_BASE> replaced with <CONDITIONAL_XXXX> instead; 

				EXEC sp_executesql
					@conditionalVersionXXX; 
			END;
			GO 

			IF dbo.engine_version >= xxy BEGIN 
				-- etc... 
			END
			GO

		And, yeah, I think that option B makes the most sense. 


	Also, one thing I might have to account for would be situations where someone does somethign like: 

--## CONDITIONAL_BASE ## don't do anything on older versions..

--## CONDITIONAL_VERSION(>= 11.7)
	DECLARE @something sysname;
	SELECT @something = blah;

	PRINT N'Something is ' + @something + N' with this version of SQL - but it''s not displayed for lower-level versions';
--## CONDITIONAL_END

	Where... the rub/challenge with the above is that I can't do soemthing like REPLACE('some white space only', '<CONDITIONAL VERSION 11.7+>')
		Cuz... that'd be a friggin nightmare. 
			In which case, I'll have to detect/determine within C# that the 'capture' for the base is ... blank/white-space
				AND, if that's the case, I'll need to grab the LINE-NUMBERS of the 'base' ... and replace those line-numbers with the replacement stuff. 

*/