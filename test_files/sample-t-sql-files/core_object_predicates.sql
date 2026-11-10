/*


*/

USE [admindb];
GO

IF OBJECT_ID('dbo.[core_object_predicates]','P') IS NOT NULL
	DROP PROC dbo.[core_object_predicates];
GO

CREATE PROC dbo.[core_object_predicates]
	@Databases				nvarchar(MAX) = NULL,
	@Tables					nvarchar(MAX) = NULL, 
	@Indexes				nvarchar(MAX) = NULL, 
	@JoinPredicates			nvarchar(MAX) OUTPUT, 
	@FilterPredicates		nvarchar(MAX) OUTPUT
AS
    SET NOCOUNT ON; 

	-- {copyright}
	
	SET @Databases = NULLIF(@Databases, N'');
	SET @Tables = NULLIF(@Tables, N'');
	SET @Indexes = NULLIF(@Indexes, N'');

	SET @JoinPredicates = N''; 
	SET @FilterPredicates = N'';

	DECLARE @crlftab nchar(3) = NCHAR(13) + NCHAR(10) + NCHAR(9);
	DECLARE @outcome int = 0, @rowId int = NULL;

	-- MKC: Copy + Paste + tweak from logic for dbo.core_predicates (i.e., cop vs cp and object_ids in @Filters)
	IF @Databases IS NOT NULL BEGIN 
		DECLARE @databasesValues table (
			[row_id] int IDENTITY(1,1) NOT NULL, 
			[databases_value] sysname NOT NULL 
		); 

		INSERT INTO @databasesValues ([databases_value])
		SELECT [result] FROM dbo.[split_string](@Databases, N',', 1);		

		INSERT INTO [#ts_cop_databases] ([database], [exclude])
		SELECT 
			CASE WHEN [databases_value] LIKE N'-%' THEN RIGHT([databases_value], LEN([databases_value]) -1) ELSE [databases_value] END [database],
			CASE WHEN [databases_value] LIKE N'-%' THEN 1 ELSE 0 END [exclude]
		FROM 
			@databasesValues 
		WHERE 
			[databases_value] NOT LIKE N'%{%';

		IF EXISTS (SELECT NULL FROM @databasesValues WHERE [databases_value] LIKE N'%{%') BEGIN 
			DECLARE @databasesToken sysname, @dbTokenAbsolute sysname;
			DECLARE @databasesXml xml;

			DECLARE [walker] CURSOR LOCAL FAST_FORWARD FOR 
			SELECT 
				[row_id], 
				[databases_value]
			FROM 
				@databasesValues 
			WHERE 
				[databases_value] LIKE N'%{%';
			
			OPEN [walker];
			FETCH NEXT FROM [walker] INTO @rowId, @databasesToken;
			
			WHILE @@FETCH_STATUS = 0 BEGIN
				
				SET @outcome = 0;
				SET @databasesXml = NULL;
				SELECT @dbTokenAbsolute = CASE WHEN @databasesToken LIKE N'-%' THEN RIGHT(@databasesToken, LEN(@databasesToken) -1) ELSE @databasesToken END;

				EXEC @outcome = dbo.[list_databases_matching_token]
					@Token = @dbTokenAbsolute,
					@SerializedOutput = @databasesXml OUTPUT;

				IF @outcome <> 0 
					RETURN @outcome; 

				WITH shredded AS ( 
					SELECT
						[data].[row].value('@id[1]', 'int') [row_id], 
						[data].[row].value('.[1]', 'sysname') [database]
					FROM 
						@databasesXml.nodes('//database') [data]([row])
				) 
				
				INSERT INTO [#ts_cop_databases] ([database], [exclude])
				SELECT 
					[database], 
					CASE WHEN @databasesToken LIKE N'-%' THEN 1 ELSE 0 END [exclude]
				FROM 
					shredded
				WHERE 
					[database] NOT IN (SELECT [database] FROM [#ts_cop_databases])
				ORDER BY 
					[row_id];
				
				FETCH NEXT FROM [walker] INTO @rowId, @databasesToken;
			END;
			
			CLOSE [walker];
			DEALLOCATE [walker];
		END;

		UPDATE [x] 
		SET 
			[x].[database_id] = [d].[database_id]
		FROM 
			[#ts_cop_databases] [x]
			INNER JOIN sys.[databases] [d] ON [x].[database] = [d].[name]
		WHERE 
			[x].[database] IS NOT NULL;



	END;

	SET @JoinPredicates = ISNULL(@JoinPredicates, N'');
	SET @FilterPredicates = ISNULL(@FilterPredicates, N'');

	RETURN 0;
GO