--##COMMENT: This is a build file only (i.e., it stores upgrade/install directives + place-holders for code to drop into admindb, etc.)
--##ROOT: D:\Dropbox\Repositories\S4\
--##OUTPUT: ~\testproject_latest.sql
SET NOCOUNT ON;
/*

	REFERENCE:
		- License, documentation, and source code at: 
			https://github.com/overachiever-productions/s4/

	NOTES:
		- This script will either install/deploy S4 version {{##S4VERSION}} or upgrade a PREVIOUSLY deployed version of S4 to {{##S4VERSION}}.
		- This script will create a new, admindb, if one is not already present on the server where this code is being run.

	Deployment Steps/Overview: 
		1. Create admindb if not already present.
		2. Create core S4 tables (and/or ALTER as needed + import data from any previous versions as needed). 
		3. Cleanup any code/objects from previous versions of S4 installed and no longer needed. 
		4. Deploy S4 version {{##S4VERSION}} code to admindb (overwriting any previous versions). 
		5. Report on current + any previous versions of S4 installed. 

*/

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 1. Create admindb if/as needed: 
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON; -- SQLCMD defaults to OFF. 

USE [master];
GO

IF NOT EXISTS (SELECT NULL FROM master.sys.databases WHERE [name] = 'admindb') BEGIN
	CREATE DATABASE [admindb];  -- TODO: look at potentially defining growth size details - based upon what is going on with model/etc. 

	ALTER AUTHORIZATION ON DATABASE::[admindb] TO sa;

	ALTER DATABASE [admindb] SET RECOVERY SIMPLE;  -- i.e., treat like master/etc. 
END;
GO

USE [master];
GO

IF EXISTS (SELECT NULL FROM sys.databases WHERE [name] = N'admindb' AND [is_broker_enabled] = 1) BEGIN
	
	IF EXISTS (SELECT NULL FROM sys.[dm_exec_requests] WHERE [database_id] = DB_ID(N'admindb') AND [command] LIKE N'%PIPEOPS%') BEGIN
		RAISERROR(N'Cannot disable Service Broker for admindb because there are active requests using it. Please investigate and resolve before re-running this script.', 21, 1) WITH LOG;
	END;

	ALTER DATABASE [admindb] SET DISABLE_BROKER WITH ROLLBACK AFTER 2 SECONDS; -- not needed, so no sense having it enabled (whereas, the model db on most systems has broker enabled). 
END;
GO

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 2. Core Functionality and Tables:
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\get_engine_version.sql

-----------------------------------
--##FILE: Common\split_string.sql

-----------------------------------
--##FILE: Common\internal\get_s4_version.sql


USE [admindb];
GO

IF OBJECT_ID('dbo.version_history', 'U') IS NULL BEGIN

	CREATE TABLE dbo.version_history (
		version_id int IDENTITY(1,1) NOT NULL, 
		version_number varchar(20) NOT NULL, 
		[description] nvarchar(200) NULL, 
		deployed datetime NOT NULL CONSTRAINT DF_version_info_deployed DEFAULT GETDATE(), 
		CONSTRAINT PK_version_info PRIMARY KEY CLUSTERED (version_id)
	);

	EXEC sys.sp_addextendedproperty
		@name = 'S4',
		@value = 'TRUE',
		@level0type = 'Schema',
		@level0name = 'dbo',
		@level1type = 'Table',
		@level1name = 'version_history';
END;

DECLARE @CurrentVersion varchar(20) = N'{{##S4VERSION}}';

-- Add previous details if any are present: 
DECLARE @version sysname; 
DECLARE @objectId int;
DECLARE @createDate datetime;
SELECT @objectId = [object_id], @createDate = create_date FROM master.sys.objects WHERE [name] = N'dba_DatabaseBackups_Log';
SELECT @version = CAST([value] AS sysname) FROM master.sys.extended_properties WHERE major_id = @objectId AND [name] = 'Version';

IF NULLIF(@version,'') IS NOT NULL BEGIN
	IF NOT EXISTS (SELECT NULL FROM dbo.version_history WHERE [version_number] = @version) BEGIN
		INSERT INTO dbo.version_history (version_number, [description], deployed)
		VALUES ( @version, N'Found during deployment of ' + @CurrentVersion + N'.', @createDate);
	END;
END;
GO

-----------------------------------
--##FILE: Common\tables\numbers.sql

-----------------------------------
--##FILE: Common\tables\options.sql

-----------------------------------
--##FILE: Common\tables\backup_log.sql

-----------------------------------
--##FILE: Common\tables\restore_log.sql

-----------------------------------
--##FILE: Common\tables\corruption_check_history.sql

-----------------------------------
--##FILE: Common\tables\settings.sql

-----------------------------------
--##FILE: Common\tables\alert_responses.sql

-----------------------------------
--##FILE: Common\tables\alert_states.sql

-----------------------------------
--##FILE: Common\tables\alert_state_details.sql

-----------------------------------
--##FILE: Common\tables\eventstore_extractions.sql

-----------------------------------
--##FILE: Common\tables\eventstore_settings.sql

-----------------------------------
--##FILE: Common\tables\eventstore_report_preferences.sql

-----------------------------------
--##FILE: Common\tables\killed_processes.sql

-----------------------------------
--##FILE: Common\tables\code_library.sql

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 3. Cleanup and remove objects from previous versions (start by creating/adding dbo.drop_obsolete_objects and other core 'helper' code)
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\internal\drop_obsolete_objects.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- admindb objects:
------------------------------------------------------------------------------------------------------------------------------------------------------

DECLARE @olderObjects xml = CONVERT(xml, N'
<list>
    <entry schema="dbo" name="server_synchronization_checks" type="P" comment="v4.9 - .5.0 renamed noun_noun_check sprocs for HA monitoring to verify_noun_noun">
        <check>
            <statement>SELECT NULL FROM msdb.dbo.[sysjobsteps] WHERE [command] LIKE ''%server_synchronization_checks%''</statement>
            <warning>WARNING: v4.9 to v5.0+ name-change detected. Job Steps with calls to dbo.server_synchronization_checks were found. Please update to call dbo.verify_server_synchronization instead.</warning>
        </check>
    </entry>
    <entry schema="dbo" name="job_synchronization_checks" type="P" comment="v4.9 - .5.0 renamed noun_noun_check sprocs for HA monitoring to verify_noun_noun">
        <check>
            <statement>SELECT NULL FROM msdb.dbo.[sysjobsteps] WHERE [command] LIKE ''%job_synchronization_checks%''</statement>
            <warning>WARNING: v4.9 to v5.0+ name-change detected. Job Steps with calls to dbo.job_synchronization_checks were found. Please update to call dbo.verify_job_synchronization instead.</warning>
        </check>
    </entry>
    <entry schema="dbo" name="data_synchronization_checks" type="P" comment="v4.9 - .5.0 renamed noun_noun_check sprocs for HA monitoring to verify_noun_noun">
        <check>
            <statement>SELECT NULL FROM msdb.dbo.[sysjobsteps] WHERE [command] LIKE ''%data_synchronization_checks%''</statement>
            <warning>WARNING: v4.9 to v5.0+ name-change detected. Job Steps with calls to dbo.data_synchronization_checks were found. Please update to call dbo.verify_data_synchronization instead.</warning>
        </check>
    </entry>
    
    <entry schema="dbo" name="get_time_vector" type="P" comment="v5.6 Vector Standardization (cleanup)." />
    <entry schema="dbo" name="get_vector" type="P" comment="v5.6 Vector Standardization (cleanup)." />
    <entry schema="dbo" name="get_vector_delay" type="P" comment="v5.6 Vector Standardization (cleanup)." />

    <entry schema="dbo" name="script_server_logins" type="P" comment="v6.2 refactoring." />
    <entry schema="dbo" name="print_logins" type="P" comment="v6.2 refactoring." />
    <entry schema="dbo" name="script_server_configuration" type="P" comment="v6.2 refactoring." />
    <entry schema="dbo" name="print_configuration" type="P" comment="v6.2 refactoring." />

    <entry schema="dbo" name="respond_to_db_failover" type="P" comment="v6.5 refactoring (changed to dbo.process_synchronization_failover)" />

	<entry schema="dbo" name="server_trace_flags" type="U" comment="v6.6 - Direct Query for Trace Flags vs delayed/table-checks." />

	<entry schema="dbo" name="script_configuration" type="P" comment="v8.0 - Renamed to dbo.script_server_configuration - better alignment with scope." />

	<entry schema="dbo" name="fix_orphaned_logins" type="P" comment="v11.1 - Renamed from dbo.fix_orphaned_logins - which doesn''t make sense - we''re fixing USERs." />
	<entry schema="dbo" name="alter_jobstep_body" type="P" comment="v11.1 - Renamed from dbo.alter_jobstep_body - to toy with test of &lt;object&gt;-&lt;verb&gt; naming conventions for some things?" />

	<entry schema="dbo" name="plancache_columns_by_index" type="P" comment="v12.1 refactoring." />
	<entry schema="dbo" name="plancache_columns_by_table" type="P" comment="v12.1 refactoring." />
	<entry schema="dbo" name="plancache_metrics_for_index" type="P" comment="v12.1 refactoring." />

	<entry schema="dbo" name="list_database_details" type="P" comment="v12.6 refactoring." />

	<entry schema="dbo" name="disable_and_script_logins" type="P" comment="v13.0 refactoring." />
	<entry schema="dbo" name="disable_and_script_job_states" type="P" comment="v13.0 refactoring." />
    <entry schema="dbo" name="list_problem_heaps" type="P" comment="Switched to dbo.list_heap_problems (to avoid intellisense ''collisions'' with dbo.list_processes)." />
	<entry schema="dbo" name="view_querystore_consumers" type="P" comment="Simplfieid name - to querystore_consumers." />
	<entry schema="dbo" name="execute_uncatchable_command" type="P" comment="Replaced via dbo.execute_command." />
	<entry schema="dbo" name="kill_blocking_process_snapshots" type="P" comment="v14.0 refactoring." />
	<entry schema="dbo" name="kill_connections_by_statement" type="P" comment="v15.0 cleanup." />
	<entry schema="dbo" name="kill_connections_by_hostname" type="P" comment="v15.0 cleanup." />
	<entry schema="dbo" name="list_running_jobs" type="P" comment="v14.2 refactoring." />
	<entry schema="dbo" name="translate_io_perfcounters" type="P" comment="v14.5 refactoring. (removed ''perf'' from name)." />
	<entry schema="dbo" name="print_long_string" type="P" comment="v14.7 refactoring. (removed ''_long_'' from name)." />
</list>');

EXEC dbo.drop_obsolete_objects @olderObjects, N'admindb';
GO

-----------------------------------
-- v7.0+ - Conversion of [tokens] to {tokens}. (Breaking Change - Raises warnings/alerts via SELECT statements). 
IF (SELECT admindb.dbo.get_s4_version('{{##S4VERSION}}')) < 7.0 BEGIN

	-- Replace any 'custom' token definitions in dbo.settings: 
	DECLARE @tokenChanges table (
		setting_id int NOT NULL, 
		old_setting_key sysname NOT NULL, 
		new_setting_key sysname NOT NULL 
	);

	UPDATE [dbo].[settings]
	SET 
		[setting_key] = REPLACE(REPLACE([setting_key], N']', N'}'), N'[', N'{')
	OUTPUT 
		[Deleted].[setting_id], [Deleted].[setting_key], [Inserted].[setting_key] INTO @tokenChanges
	WHERE 
		[setting_key] LIKE N'~[%~]' ESCAPE '~';


	IF EXISTS (SELECT NULL FROM @tokenChanges) BEGIN 

		SELECT 
			N'WARNING: dbo.settings.setting_key CHANGED from pre 7.0 [token] syntax to 7.0+ {token} syntax' [WARNING], 
			[setting_id], 
			[old_setting_key], 
			[new_setting_key]
		FROM 
			@tokenChanges
	END;

	-- Raise alerts/warnings about any Job-Steps on the server with old-style [tokens] instead of {tokens}:
	DECLARE @oldTokens table ( 
		old_token_id int IDENTITY(1,1) NOT NULL, 
		token_pattern sysname NOT NULL, 
		is_custom bit DEFAULT 1
	); 

	INSERT INTO @oldTokens (
		[token_pattern], [is_custom]
	)
	VALUES
		(N'%~[ALL~]%', 0),
		(N'%~[SYSTEM~]%', 0),
		(N'%~[USER~]%', 0),
		(N'%~[READ_FROM_FILESYSTEM~]%', 0), 
		(N'%~[READ_FROM_FILE_SYSTEM~]%', 0), 
		(N'%~[DEFAULT~]%', 0);

	INSERT INTO @oldTokens (
		[token_pattern]
	)
	SELECT DISTINCT
		N'%~' + REPLACE([setting_key], N']', N'~]') + N'%'
	FROM 
		[admindb].[dbo].[settings] 
	WHERE 
		[setting_key] LIKE '~[%~]' ESCAPE '~';

	WITH matches AS ( 
		SELECT 
			js.[job_id], 
			js.[step_id], 
			js.[command], 
			js.[step_name],
			x.[token_pattern]
		FROM 
			[msdb].dbo.[sysjobsteps] js 
			INNER JOIN @oldTokens x ON js.[command] LIKE x.[token_pattern] ESCAPE N'~'
		WHERE 
			js.[subsystem] = N'TSQL'
	)

	SELECT 
		N'WARNING: SQL Server Agent Job-Step uses PRE-7.0 [tokens] which should be changed to {token} syntax instead.' [WARNING],
		j.[name] [job_name], 
		--	j.[job_id], 
		CAST(m.[step_id] AS sysname) + N' - ' + m.[step_name] [Job-Step-With-Invalid-Token],
		N'TASK: Manually Replace ' + REPLACE(REPLACE(m.[token_pattern], N'~', N''), N'%', N'') 
			+ N' with ' + REPLACE(REPLACE(( ( REPLACE(REPLACE(m.[token_pattern], N'~', N''), N'%', N'') ) ), N']', N'}'), N'[', N'{') + '.' [Task-To-Execute-Manually]
		--m.[command]
	FROM 
		[matches] m 
		INNER JOIN [msdb].dbo.[sysjobs] j ON m.[job_id] = j.[job_id];
END;
GO

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 4. Deploy new/updated code.
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

USE [admindb];
GO

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Types:
------------------------------------------------------------------------------------------------------------------------------------------------------
--##FILE: Common\Types\backup_history_entry.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Advanced S4 Error-Handling Capabilities:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\Setup\enable_advanced_capabilities.sql

-----------------------------------
--##FILE: Common\Setup\disable_advanced_capabilities.sql

-----------------------------------
--##FILE: Common\Setup\verify_advanced_capabilities.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Common Utilities:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\engine_version().sql

-----------------------------------
--##FILE: Common\Internal\extract_option.sql

-----------------------------------
--##FILE: Common\Internal\extract_parameter_option.sql

-----------------------------------
--##FILE: Common\base64_encode.sql

-----------------------------------
--##FILE: Common\Internal\remove_whitespace().sql

-----------------------------------
--##FILE: Common\format_text_width().sql

-----------------------------------
--##FILE: Common\format_xml_string().sql

-----------------------------------
--##FILE: Common\Internal\check_paths.sql

-----------------------------------
--##FILE: Common\database_defaults().sql

-----------------------------------
--##FILE: Common\database_scoped_defaults().sql

-----------------------------------
--##FILE: Common\Internal\verify_directory_access.sql

-----------------------------------
--##FILE: Common\Internal\normalize_file_path.sql

-----------------------------------
--##FILE: Common\Internal\load_default_path.sql

-----------------------------------
--##FILE: Common\Internal\load_default_setting.sql

-----------------------------------
--##FILE: Common\Internal\shred_resources.sql

-----------------------------------
--##FILE: Common\Internal\is_system_database.sql

-----------------------------------
--##FILE: Common\Internal\parse_vector.sql

-----------------------------------
--##FILE: Common\Internal\translate_vector.sql

-----------------------------------
--##FILE: Common\Internal\translate_vector_delay.sql

-----------------------------------
--##FILE: Common\Internal\translate_vector_datetime.sql

-----------------------------------
--##FILE: Common\Internal\verify_alerting_configuration.sql

-----------------------------------
--##FILE: Common\Internal\verify_directory_access.sql

-----------------------------------
--##FILE: Common\list_databases_matching_token.sql

-----------------------------------
--##FILE: Common\core_predicates.sql

-----------------------------------
--##FILE: Common\Internal\extract_waitresource.sql

-----------------------------------
--##FILE: Common\Internal\replace_dbname_tokens.sql

-----------------------------------
--##FILE: Common\Internal\format_sql_login.sql

-----------------------------------
--##FILE: Common\Internal\format_windows_login.sql

-----------------------------------
--##FILE: Common\Internal\script_sql_login.sql

-----------------------------------
--##FILE: Common\Internal\script_windows_login.sql

-----------------------------------
--##FILE: Common\Internal\create_agent_job.sql

-----------------------------------
--##FILE: Common\Internal\generate_bounding_times.sql

-----------------------------------
--##FILE: Common\Internal\inserlect_cache.sql

-----------------------------------
--##FILE: Common\targeted_databases.sql

-----------------------------------
--##FILE: Common\list_databases.sql

-----------------------------------
--##FILE: Common\format_timespan().sql

-----------------------------------
--##FILE: Common\format_number().sql

-----------------------------------
--##FILE: Common\xml_decode.sql

-----------------------------------
--##FILE: Common\get_local_timezone.sql

-----------------------------------
--##FILE: Common\get_timezone_offset_minutes.sql

-----------------------------------
--##FILE: Common\Alerting\notify_operator.sql

-----------------------------------
--##FILE: Common\Alerting\format_html_email.sql

-----------------------------------
--##FILE: Common\format_hex_string().sql

-----------------------------------
--##FILE: Common\format_hex_html().sql

-----------------------------------
--##FILE: S4 Utilities\print_string.sql

-----------------------------------
--##FILE: S4 Utilities\extract_dynamic_code_lines.sql

-----------------------------------
--##FILE: S4 Utilities\count_matches.sql

-----------------------------------
--##FILE: Common\Internal\transient_error_occurred.sql

-----------------------------------
--##FILE: Code Library\deploy_codelibrary_file.sql

-----------------------------------
--##FILE: Code Library\verify_codelibrary_file.sql

-----------------------------------
--##FILE: Common\execute_command.sql

-----------------------------------
--##FILE: Common\execute_powershell.sql

-----------------------------------
--##FILE: Common\execute_per_database.sql

-----------------------------------
--##FILE: Common\execute_per_database_errors.sql

-----------------------------------
--##FILE: Common\Internal\establish_directory.sql

-----------------------------------
--##FILE: S4 Restore\Utilities\load_header_details.sql

-----------------------------------
--##FILE: Common\Internal\load_backup_database_names.sql

-----------------------------------
--##FILE: S4 Utilities\shred_string.sql

-----------------------------------
--##FILE: Common\Internal\get_executing_dbname.sql

-----------------------------------
--##FILE: Common\Internal\load_id_for_normalized_name.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Backups:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Restore\Utilities\load_backup_files.sql

-----------------------------------
--##FILE: S4 Backups\Utilities\log_backup_history_detail.sql

-----------------------------------
--##FILE: S4 Backups\Utilities\validate_retention.sql

-----------------------------------
--##FILE: S4 Backups\Utilities\remove_backup_files.sql

-----------------------------------
--##FILE: S4 Backups\Utilities\remove_offsite_backup_files.sql

-----------------------------------
--##FILE: S4 Backups\backup_databases.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Code Library:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\tables\code_library.sql

-----------------------------------
--##FILE: Code Library\create_code_formatfile.sql

-----------------------------------
--##FILE: Code Library\codelibrary_file.sql

-----------------------------------
--##FILE: Code Library\initialize_codelibrary.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Configuration:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Configuration\update_server_name.sql

-----------------------------------
--##FILE: S4 Configuration\force_removal_of_tempdb_file.sql

-----------------------------------
--##FILE: S4 Configuration\configure_tempdb_files.sql

-----------------------------------
--##FILE: S4 Configuration\script_server_configuration.sql

-----------------------------------
--##FILE: S4 Configuration\export_server_configuration.sql

-----------------------------------
--##FILE: S4 Configuration\backup_server_certificate.sql

-----------------------------------
--##FILE: S4 Configuration\create_server_certificate.sql

-----------------------------------
--##FILE: S4 Configuration\restore_server_certificate.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\configure_instance.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\configure_database_mail.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\enable_alerts.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\enable_alert_filtering.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\manage_server_history.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\enable_disk_monitoring.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\create_backup_jobs.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\create_restore_test_job.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\create_index_maintenance_jobs.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\create_consistency_checks_job.sql

-----------------------------------
--##FILE: S4 Configuration\Setup\define_masterkey_encryption.sql

-----------------------------------
--##FILE: S4 Configuration\script_dbfile_movement_template.sql

-----------------------------------
--##FILE: S4 Configuration\Security\script_login.sql

-----------------------------------
--##FILE: S4 Configuration\Security\script_server_role.sql

-----------------------------------
--##FILE: S4 Configuration\Security\script_logins.sql

-----------------------------------
--##FILE: S4 Configuration\Security\fix_orphaned_users.sql

-----------------------------------
--##FILE: S4 Configuration\Security\drop_orphaned_users.sql

-----------------------------------
--##FILE: S4 Configuration\Security\export_server_logins.sql

-----------------------------------
--##FILE: S4 Configuration\Security\prevent_user_access.sql

-----------------------------------
--##FILE: S4 Configuration\Security\script_security_mappings.sql

-----------------------------------
--##FILE: S4 Configuration\Security\import_security_mappings.sql

-----------------------------------
--##FILE: S4 Configuration\script_server_certificate.sql

-----------------------------------
--##FILE: S4 Configuration\restore_encoded_certificate.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Restores:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Restore\Utilities\parse_backup_filename_timestamp.sql

-----------------------------------
--##FILE: S4 Restore\Reports\report_rpo_restore_violations.sql

-----------------------------------
--##FILE: S4 Restore\restore_databases.sql

-----------------------------------
--##FILE: S4 Restore\Tools\copy_database.sql

-----------------------------------
--##FILE: S4 Restore\apply_logs.sql

-----------------------------------
--##FILE: S4 Restore\Reports\list_recovery_metrics.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Performance
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Performance\list_top.sql

-----------------------------------
--##FILE: S4 Performance\list_processes.sql

-----------------------------------
--##FILE: S4 Performance\list_parallel_processes.sql

-----------------------------------
--##FILE: S4 Performance\list_transactions.sql

-----------------------------------
--##FILE: S4 Performance\list_collisions.sql

-----------------------------------
--##FILE: S4 Performance\list_cpu_history.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Migration:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Migration\initialize_migration.sql

-----------------------------------
--##FILE: S4 Migration\finalize_migration.sql

-----------------------------------
--##FILE: S4 Migration\disable_jobs.sql

-----------------------------------
--##FILE: S4 Migration\disable_logins.sql

-----------------------------------
--##FILE: S4 Migration\script_job_states.sql

-----------------------------------
--##FILE: S4 Migration\script_login_states.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- SQL Server Agent Jobs
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Jobs\running_jobs.sql

-----------------------------------
--##FILE: S4 Jobs\job_schedules().sql

-----------------------------------
--##FILE: S4 Jobs\is_job_running.sql

-----------------------------------
--##FILE: S4 Jobs\translate_program_name_to_agent_job.sql

-----------------------------------
--##FILE: S4 Jobs\get_last_job_completion.sql

-----------------------------------
--##FILE: S4 Jobs\get_last_job_completion_by_session_id.sql

-----------------------------------
--##FILE: S4 Jobs\job_synchronization\jobstep_body_alter.sql

-----------------------------------
--##FILE: S4 Jobs\job_synchronization\jobstep_body_get.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Diagnostics
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Diagnostics\aggregated_errorlog.sql

-----------------------------------
--##FILE: S4 Diagnostics\vlf_counts.sql

-----------------------------------
--##FILE: S4 Diagnostics\compute_details.sql

-----------------------------------
--##FILE: S4 Diagnostics\disabled_constraints.sql

-----------------------------------
--##FILE: S4 Diagnostics\database_settings().sql

-----------------------------------
--##FILE: S4 Diagnostics\database_details.sql

-----------------------------------
--##FILE: S4 Diagnostics\table_sizes.sql

-----------------------------------
--##FILE: S4 Diagnostics\io_freezes.sql

-----------------------------------
--##FILE: S4 Diagnostics\server_configuration.sql

-----------------------------------
--##FILE: S4 Diagnostics\Connections\thesevensets_server_useroptions.sql

-----------------------------------
--##FILE: S4 Diagnostics\Connections\thesevensets_database_settings.sql

-----------------------------------
--##FILE: S4 Diagnostics\Connections\thesevensets_problem_connections.sql

-----------------------------------
--##FILE: S4 Diagnostics\Connections\thesevensets_problem_objects.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\filtered_index_obstacles.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\script_indexes.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\list_index_metrics.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\help_index.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\list_heaps.sql

-----------------------------------
--##FILE: S4 Diagnostics\Indexes\list_heap_problems.sql

-----------------------------------
--##FILE: S4 Diagnostics\PlanCache\plancache_shred_columns_by_table.sql

-----------------------------------
--##FILE: S4 Diagnostics\PlanCache\plancache_shred_metrics_for_index.sql

-----------------------------------
--##FILE: S4 Diagnostics\PlanCache\plancache_shred_columns_by_index.sql

-----------------------------------
--##FILE: S4 Diagnostics\PlanCache\plancache_shred_statistics_by_table.sql

-----------------------------------
--##FILE: S4 Diagnostics\Performance\querystats_consumers.sql

-----------------------------------
--##FILE: S4 Diagnostics\Schema\column_widths.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\escalated_server_permissions.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\list_sysadmins_and_owners.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\list_orphaned_users.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\list_login_permissions.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\nonsafe_clr_assemblies.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\server_role_members.sql

-----------------------------------
--##FILE: S4 Diagnostics\Security\server_permissions.sql

-----------------------------------
--##FILE: S4 Diagnostics\extract_log_events.sql

-----------------------------------
--##FILE: S4 Diagnostics\Storage\system_disks.sql

-----------------------------------
--##FILE: S4 Diagnostics\Storage\directory_sizing.sql

-----------------------------------
--##FILE: S4 Diagnostics\Storage\file_stalls.sql

-----------------------------------
--##FILE: S4 Diagnostics\TempDb\tempdb_details.sql

-----------------------------------
--##FILE: S4 Diagnostics\QueryStore\querystore_details.sql

-----------------------------------
--##FILE: S4 Diagnostics\QueryStore\querystore_consumers.sql

-----------------------------------
--##FILE: S4 Diagnostics\QueryStore\view_querystore_counts.sql

-----------------------------------
--##FILE: S4 Diagnostics\QueryStore\querystore_compilation_consumers.sql

-----------------------------------
--##FILE: S4 Diagnostics\QueryStore\querystore_list_forced_plans.sql

-----------------------------------
--##FILE: S4 Diagnostics\TempDb\versionstore_consumers.sql

-----------------------------------
--##FILE: S4 Diagnostics\TempDb\versionstore_generators.sql

-----------------------------------
--##FILE: S4 Diagnostics\Waits\signal_waits.sql

-----------------------------------
--##FILE: S4 Diagnostics\Waits\wait_stats.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Monitoring:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Monitoring\verify_backup_execution.sql

-----------------------------------
--##FILE: S4 Monitoring\verify_database_configurations.sql

-----------------------------------
--##FILE: S4 Monitoring\verify_drivespace.sql

-----------------------------------
--##FILE: S4 Monitoring\process_alerts.sql

-----------------------------------
--##FILE: S4 Monitoring\monitor_transaction_durations.sql

-----------------------------------
--##FILE: S4 Monitoring\verify_cpu_thresholds.sql

-----------------------------------
--##FILE: S4 Monitoring\verify_ple_thresholds.sql

-----------------------------------
--##FILE: S4 Monitoring\verify_dev_configurations.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Extended Events
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Event Store\utilities\list_xe_sessions.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_get_target_by_key.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_translate_error_token.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_initialize_extraction.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_finalize_extraction.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_extract_session_xml.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_etl_session.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_etl_processor.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_verify_jobs.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_setup_session.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_data_cleanup.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_timebounded_counts.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_heatmap_frame.sql

-----------------------------------
--##FILE: Event Store\setup\eventstore_enable_all_errors.sql

-----------------------------------
--##FILE: Event Store\setup\eventstore_enable_blocked_processes.sql

-----------------------------------
--##FILE: Event Store\setup\eventstore_enable_deadlocks.sql

-----------------------------------
--##FILE: Event Store\setup\eventstore_enable_large_sql.sql

-----------------------------------
--##FILE: Event Store\etl\eventstore_etl_all_errors.sql

-----------------------------------
--##FILE: Event Store\etl\eventstore_etl_blocked_processes.sql

-----------------------------------
--##FILE: Event Store\etl\eventstore_etl_deadlocks.sql

-----------------------------------
--##FILE: Event Store\etl\eventstore_etl_large_sql.sql

-----------------------------------
--##FILE: Event Store\core\eventstore_report_predicates.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_get_report_preferences.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_all_errors_counts.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_all_errors_chronology.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_all_errors_heatmap.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_all_errors_problems.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_blocked_processes_chronology.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_blocked_processes_counts.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_deadlock_counts.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_large_sql_chronology.sql

-----------------------------------
--##FILE: Event Store\reports\eventstore_report_large_sql_counts.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Maintenance
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Maintenance\check_database_consistency.sql

-----------------------------------
--##FILE: S4 Maintenance\clear_stale_jobsactivity.sql

-----------------------------------
--##FILE: S4 Maintenance\Automated Log Shrinking\list_logfile_sizes.sql

-----------------------------------
--##FILE: S4 Maintenance\Automated Log Shrinking\shrink_logfiles.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Additional Utilities
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Utilities\normalize_text.sql

-----------------------------------
--##FILE: S4 Utilities\extract_statement.sql

-----------------------------------
--##FILE: S4 Utilities\extract_code_lines.sql

-----------------------------------
--##FILE: S4 Utilities\is_xml_empty.sql

-----------------------------------
--##FILE: S4 Utilities\refresh_code.sql

-----------------------------------
--##FILE: S4 Utilities\extract_directory_from_fullpath.sql

-----------------------------------
--##FILE: S4 Utilities\extract_filename_from_fullpath.sql

-----------------------------------
--##FILE: S4 Utilities\count_rows.sql

-----------------------------------
--##FILE: S4 Utilities\dump_module_code.sql

-----------------------------------
--##FILE: S4 Utilities\extract_matches.sql

-----------------------------------
--##FILE: Kill Store\kill_blocking_processes.sql

-----------------------------------
--##FILE: Kill Store\kill_long_running_processes.sql

-----------------------------------
--##FILE: S4 Utilities\translate_characters.sql

-----------------------------------
--##FILE: S4 Utilities\s3\aws3_verify_configuration.sql

-----------------------------------
--##FILE: S4 Utilities\s3\aws3_install_modules.sql

-----------------------------------
--##FILE: S4 Utilities\s3\aws3_initialize_profile.sql

-----------------------------------
--##FILE: S4 Utilities\s3\aws3_list_buckets.sql

-----------------------------------
--##FILE: S4 Utilities\s3\aws3_verify_bucket_write.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Idioms
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Idioms\idiom_for_batched_operation.sql

-----------------------------------
--##FILE: S4 Idioms\blueprints\blueprint_for_batched_operation.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
--- Resource Governor
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Kill Store\kill_resource_governor_connections.sql

-----------------------------------
--##FILE: S4 Resource Governor\resource_governor_compute_metrics.sql

-----------------------------------
--##FILE: S4 Resource Governor\resource_governor_io_metrics.sql


------------------------------------------------------------------------------------------------------------------------------------------------------
--- Capacity Planning
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Capacity Planning\extraction\translate_cpu_counters.sql

-----------------------------------
--##FILE: S4 Capacity Planning\extraction\translate_io_counters.sql

-----------------------------------
--##FILE: S4 Capacity Planning\extraction\translate_memory_counters.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_cpu_and_sql_exception_percentages.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_cpu_and_sql_threshold_exceptions.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_cpu_percent_of_percent_load.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_io_percent_of_percent_load.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_io_threshold_exceptions.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_memory_percent_of_percent_load.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_throughput_percent_of_percent_load.sql

-----------------------------------
--##FILE: S4 Capacity Planning\report_trace_continuity.sql


------------------------------------------------------------------------------------------------------------------------------------------------------
-- High-Availability (Setup, Monitoring, and Failover):
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 High Availability\server_trace_flags.sql

-----------------------------------
-- v6.6 Changes to PARTNER (if present):
IF EXISTS (SELECT NULL FROM sys.servers WHERE UPPER([name]) = N'PARTNER' AND [is_linked] = 1) BEGIN 
	IF NOT EXISTS (SELECT NULL FROM sys.[sysservers] WHERE UPPER([srvname]) = N'PARTNER' AND [rpc] = 1) BEGIN
        EXEC master.dbo.sp_serveroption 
	        @server = N'PARTNER', 
	        @optname = N'rpc', 
	        @optvalue = N'true';		

		PRINT N'Enabled RPC on PARTNER (for v6.6+ compatibility).';
	END;

	IF NOT EXISTS (SELECT NULL FROM sys.[sysservers] WHERE UPPER([srvname]) = N'PARTNER' AND [rpcout] = 1) BEGIN
        EXEC master.dbo.sp_serveroption 
	        @server = N'PARTNER', 
	        @optname = N'rpc out', 
	        @optvalue = N'true';
			
		PRINT N'Enabled RPC_OUT on PARTNER (for v6.6+ compatibility).';
	END;
END;

-----------------------------------
--##FILE: S4 High Availability\list_synchronizing_databases.sql

-----------------------------------
--##FILE: S4 High Availability\is_primary_server.sql

-----------------------------------
--##FILE: S4 High Availability\is_primary_database.sql

-----------------------------------
--##FILE: S4 High Availability\preferred_secondary.sql

-----------------------------------
--##FILE: S4 High Availability\compare_jobs.sql

-----------------------------------
--##FILE: S4 High Availability\Failover\process_synchronization_status.sql

-----------------------------------
--##FILE: S4 High Availability\Failover\process_synchronization_failover.sql

-----------------------------------
--##FILE: S4 High Availability\Failover\process_synchronization_server_start.sql

-----------------------------------
--##FILE: S4 High Availability\Failover\verify_job_states.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\Internal\populate_trace_flags.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\Internal\verify_online.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\Internal\verify_partner.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\verify_job_synchronization.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\verify_server_synchronization.sql

-----------------------------------
--##FILE: S4 High Availability\Monitoring\verify_data_synchronization.sql

-----------------------------------
--##FILE: S4 High Availability\Setup & Configuration\add_synchronization_partner.sql

-----------------------------------
--##FILE: S4 High Availability\Setup & Configuration\add_failover_processing.sql

-----------------------------------
--##FILE: S4 High Availability\Setup & Configuration\create_sync_check_jobs.sql

-----------------------------------
--##FILE: S4 High Availability\Setup & Configuration\verify_synchronization_setup.sql

-----------------------------------
--##FILE: Common\Internal\list_nonaccessible_databases.sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Shreds:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: Common\Shreds\log_events_data().sql

-----------------------------------
--##FILE: Common\Shreds\directory_sizing_data().sql

-----------------------------------
--##FILE: Common\Shreds\system_disks_data().sql

------------------------------------------------------------------------------------------------------------------------------------------------------
-- Auditing:
------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------------------
--##FILE: S4 Audits\Utilities\generate_audit_signature.sql

-----------------------------------
--##FILE: S4 Audits\Utilities\generate_specification_signature.sql

-----------------------------------
--##FILE: S4 Audits\Monitoring\verify_audit_configuration.sql

-----------------------------------
--##FILE: S4 Audits\Monitoring\verify_specification_configuration.sql

----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 5. Update version_history with details about current version (i.e., if we got this far, the deployment is successful). 
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
DECLARE @CurrentVersion varchar(20) = N'{{##S4VERSION}}';
DECLARE @VersionDescription nvarchar(200) = N'{{##S4VERSION-SUMMARY}}';
DECLARE @InstallType nvarchar(20) = N'Install. ';

IF EXISTS (SELECT NULL FROM dbo.[version_history] WHERE CAST(LEFT(version_number, 3) AS decimal(3,1)) >= 4)
	SET @InstallType = N'Update. ';

SET @VersionDescription = @InstallType + @VersionDescription;

-- Add current version info:
IF NOT EXISTS (SELECT NULL FROM dbo.version_history WHERE [version_number] = @CurrentVersion) BEGIN
	INSERT INTO dbo.version_history (version_number, [description], deployed)
	VALUES (@CurrentVersion, @VersionDescription, GETDATE());
END;
GO

IF EXISTS (SELECT NULL FROM sys.sql_modules WHERE [uses_quoted_identifier] = 0) BEGIN
	SELECT N'WARNING: One or more modules has [uses_quoted_identifier] = 0. Installation FAILED' [failed_installation];
END;

-----------------------------------
SELECT * FROM dbo.version_history;
GO