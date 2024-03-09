namespace tsmake.tests.unit_tests.file_tests;

[TestFixture]
public class BuildFileTests
{
    [Test]
    public void This_Is_Not_A_Test_Its_For_Debugging()
    {
        var listOfLines = new List<string>
        {
            @"-- ## :: This current, test, build file is located at: D:\Dropbox\Repositories\tsmake\~~spelunking\current.build.sql ",
            @"-- ## ROOT: ..\ ##:: ",
            @"-- ## OUTPUT: \\\piggly_wiggly.sql",
            @"-- ## :: This is a build file only (i.e., it stores upgrade/install directives + place-holders for code to drop into admindb, etc.)",
            @"/*",
            @"",
            @"	REFERENCE:",
            @"		- License, documentation, and source code at: ",
            @"			https://github.com/overachiever-productions/s4/",
            @"",
            @"/*"
        };


        var fileSystem = new Mock<IFileManager>();
        fileSystem.Setup(fs => fs.GetFileLines(It.IsAny<string>()))
            .Returns(listOfLines);

        var fileManager = new FileManager(fileSystem.Object);
        var sut = new BuildFile("build.sql", fileManager);
    }
}