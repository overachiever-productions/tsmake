namespace tsmake.tests.unit_tests.inclusion_tests;

[TestFixture]
public class IncludedDirectoryTests
{
    [Test]
    public void Included_Directory_Excludes_Explicitly_Defined_Exclusions()
    {
        var line = new Line("current.build.sql", 84, "-- ## DIRECTORY: Common ORDERBY: alpha EXCLUDE: %tables% PRIORITIES: get_engine_version%, %split_string%; get_s4_version%");
        var directive = (IncludeDirectoryDirective)line.Directive;

        //var listOfFiles = this.GetAllTestFiles();
        var listOfFiles = new List<string>
        {
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Setup\verify_advanced_capabilities.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\~~alert_response options.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\enumeration.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\extraction_mapping.sql"
        };

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(fm => fm.GetDirectoryFiles(It.IsAny<string>(), RecursionOption.TopOnly))
            .Returns(listOfFiles);
        fileManager.Setup(fm => fm.DirectoryExists(It.IsAny<string>()))
            .Returns(true);

        var sut = new IncludedDirectory(directive, fileManager.Object, @"D:\Repositories\some-repo\build", @"D:\Repositories\some-repo");

        Assert.That(sut.SourceFiles.Count, Is.EqualTo(3));
    }

    private List<string> GetAllTestFiles()
    {
        var listOfFiles = new List<string>
        {
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\~~execute command - refactoring.SQL",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\~~execute command - spelunking - EXEC execType and OUTPUTBUFFER parsing attempts.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\~~execute command tests.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\execute_command.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\execute_powershell.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\execute_uncatchable_command.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\format_number.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\format_timespan.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\get_engine_version.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\get_local_timezone.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\get_timezone_offset_minutes.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\list_databases_matching_token.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\list_databases.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\split_string.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\xml_decode.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Alerting\~~identify which rule to use - spelunking.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Alerting\~~parse_escalation_definition.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Alerting\~~translate_escalation_definitions.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\check_paths.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\create_agent_job.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\drop_obsolete_objects.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\establish_directory.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\extract_waitresource_query.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\extract_waitresource.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\format_operation_xml.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\format_sql_login.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\format_windows_login.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\generate_bounding_times.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\get_executing_dbname.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\get_s4_version.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\get_signature.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\is_system_database.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\list_nonaccessible_databases.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\load_backup_database_names.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\load_default_path.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\load_default_setting.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\load_id_for_normalized_name.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\load_session_performance_details.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\normalize_file_path.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\parse_vector.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\peek_output_buffer.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\process_bus_operation.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\remove_multiline_comments.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\replace_dbname_tokens.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\script_sql_login.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\script_windows_login.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\shred_resources.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\transient_error_occurred.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\translate_vector_datetime.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\translate_vector_delay.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\translate_vector.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Internal\verify_alerting_configuration.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Setup\disable_advanced_capabilities.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Setup\enable_advanced_capabilities.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Setup\verify_advanced_capabilities.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\~~alert_response options.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\alert_responses.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\backup_log.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\captured_metrics.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\eventstore_extractions.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\kill_blocking_processes_snapshots.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\restore_log.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\settings.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\stats_management_history.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\__UDTT Notes.txt",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\backup_history_entry.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\enumeration.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\extraction_mapping.sql"
        };

        return listOfFiles;
    }
}