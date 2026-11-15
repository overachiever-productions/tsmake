namespace tsmake.tests.unit_tests;

[TestFixture]
public class AssemblerArtifactTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Assemble_RequiresBuildArtifact(bool hasBuildOutput, bool hasFileMarker)
    {
        var fileSystem = new Mock<tsmake.IFileSystem>();
        fileSystem.Setup(fs => fs.GetFileContent("build.sql")).Returns("SELECT 1;");
        fileSystem.SetupGet(fs => fs.RootDirectory).Returns("root");
        fileSystem.SetupGet(fs => fs.RootSourceType).Returns(SourceType.Convention);

        var options = new AssemblerOptions(TokenDefinitionRegistry.NewRegistry, OperationType.Build);
        if (hasBuildOutput)
            options.AddOutputPath(new RankedString(SourceType.CommandLine, "output.sql"));
        if (hasFileMarker)
            options.AddFileMarkerPath(new RankedString(SourceType.CommandLine, "marker.txt"));

        var result = new BuildResult();
        var assembler = new Assembler(new OpsFactory(fileSystem.Object, options), options, result);

        assembler.Assemble("build.sql");

        Assert.That(result.Artifacts.OfType<BuildArtifact>().Count(), Is.EqualTo(hasBuildOutput ? 1 : 0));
        Assert.That(result.Artifacts.OfType<FileMarkerArtifact>().Count(), Is.EqualTo(hasFileMarker ? 1 : 0));
        if (hasBuildOutput)
        {
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Assembler, Is.SameAs(assembler));
        }
        else
        {
            Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(result.Exception.Message, Is.EqualTo("No BUILD Output was specified."));
        }
    }
}
