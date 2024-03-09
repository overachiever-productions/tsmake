namespace tsmake.tests.unit_tests.inclusion_tests;

[TestFixture]
public class IncludeFactoryTests
{
    [Test]
    public void This_Is_Not_A_Test_It_Is_For_Debugging()
    {
        var line = new Line("current.build.sql", 84, "-- ## DIRECTORY: Common ORDERBY: alpha EXCLUDE: %tables% PRIORITIES: get_engine_version%, %split_string%; get_s4_version%");
        var directive = (IncludeDirectoryDirective)line.Directive;

        var listOfFiles = new List<string>
        {
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Setup\verify_advanced_capabilities.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\~~alert_response options.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\enumeration.sql",
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Types\extraction_mapping.sql"
        };

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.GetDirectoryFiles(It.IsAny<string>(), RecursionOption.TopOnly))
            .Returns(listOfFiles);
        
        var fileManager = new FileManager(fileSystem.Object);

        var sut = IncludeFactory.GetInclude(directive, fileManager, @"D:\Repositories\some-repo\build", @"D:\Repositories\some-repo");
    }

    [Test]
    public void This_Also_Is_Not_A_Test()
    {
        var line = new Line("current.build.sql", 90, @"--##File: Common\tables\restore_log.sql");
        var directive = (IncludeFileDirective)line.Directive;

        var listOfFiles = new List<string>
        {
            @"D:\Dropbox\Repositories\tsmake\~~spelunking\Common\Tables\restore_log.sql",
        };

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.GetDirectoryFiles(It.IsAny<string>(), RecursionOption.TopOnly))
            .Returns(listOfFiles);

        var fileManager = new FileManager(fileSystem.Object);
    }
}