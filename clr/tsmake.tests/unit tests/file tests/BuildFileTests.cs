namespace tsmake.tests.unit_tests.file_tests;

[TestFixture]
public class BuildFileTests
{
    //[Test]
    //public void This_Is_Not_A_Test_Its_For_Debugging()
    //{
    //    var buildFileLines = new List<string>
    //    {
    //        @"-- ## :: This current, test, build file is located at: D:\Dropbox\Repositories\tsmake\~~spelunking\current.build.sql ",
    //        @"-- ## ROOT: ..\ ##:: ",
    //        @"-- ## OUTPUT: \\\piggly_wiggly.sql",
    //        @"-- ## :: This is a build file only (i.e., it stores upgrade/install directives + place-holders for code to drop into admindb, etc.)",
    //        @"/*",
    //        @"",
    //        @"	REFERENCE:",
    //        @"		- License, documentation, and source code at: ",
    //        @"			https://github.com/overachiever-productions/s4/",
    //        @"",
    //        @"/*"
    //    };

    //    var fileManager = new Mock<IFileManager>();
    //    fileManager.Setup(fm => fm.GetFileLines(It.IsAny<string>()))
    //        .Returns(buildFileLines);

    //    var sut = new BuildFile("build.sql", fileManager.Object);
    //}

    [Test]
    public void Build_File_With_No_Includes_Correctly_Identifies_Source_File_Locations()
    {
        var buildFileLines = new List<string>
        {
            @"-- ## ROOT: ..\ ##:: ",
            @"-- ## OUTPUT: \\\piggly_wiggly.sql",
            @"/*",
            @"	REFERENCE:",
            @"		- License, documentation, and source code at: ",
            @"			https://github.com/overachiever-productions/s4/",
            @"/*"
        };

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(fm => fm.GetFileLines(It.IsAny<string>()))
            .Returns(buildFileLines);

        var sut = new BuildFile("simple.build.sql", fileManager.Object);

        Assert.That(sut.Lines.Count, Is.EqualTo(7));

        Assert.That(sut.Lines[0].Location.Count, Is.EqualTo(1));
        Assert.That(sut.Lines[0].Location.Peek().FileName, Is.EqualTo("simple.build.sql"));
        Assert.That(sut.Lines[0].Location.Peek().LineNumber, Is.EqualTo(1));

        Assert.That(sut.Lines[6].Location.Count, Is.EqualTo(1));
        Assert.That(sut.Lines[6].Location.Peek().FileName, Is.EqualTo("simple.build.sql"));
        Assert.That(sut.Lines[6].Location.Peek().LineNumber, Is.EqualTo(7));
    }

    [Test]
    public void Build_File_With_Simple_Include_Identifies_Source_For_Both_Files()
    {
        var includedFileLines = new List<string>
        {
            @"Nested File - Line 1",
            @"Nested File - Line 2",
            @"Nested File - Line 3"
        };

        var line4 = new Line("simple.build.sql", 4, @"-- ## FILE: SomePath.sql ");

        var fileManager = new Mock<IFileManager>();
        fileManager.Setup(fm => fm.GetFileLines("SomePath.sql"))
            .Returns(includedFileLines);

        // Simulate processing of line #4 (i.e., assume that BuildFile processing was fine/as-expected.
        var sut = LineProcessor.ProcessLines(line4, "SomePath.sql", ProcessingType.IncludedFile, fileManager.Object, "N/A", "N/A");

        Assert.That(sut.Lines.Count, Is.EqualTo(3));

        Assert.That(sut.Lines[0].Location.Count, Is.EqualTo(2));
        Assert.That(sut.Lines[1].Location.Count, Is.EqualTo(2));
        Assert.That(sut.Lines[2].Location.Count, Is.EqualTo(2));

        var location = sut.Lines[1].Location;

        Assert.That(location.Pop().FileName, Is.EqualTo("SomePath.sql"));
        Assert.That(location.Pop().FileName, Is.EqualTo("simple.build.sql"));
    }
}