namespace tsmake.tests.integration_tests;

[TestFixture]
public class SimpleBuild()
{
    [Test]
    public void Execute_Simple1_Build()
    {
        string pwd = "D:\\Dropbox\\Repositories\\tsmake\\test_files\\simple1";
        string buildFile = "D:\\Dropbox\\Repositories\\tsmake\\test_files\\simple1\\basic.build.sql";
        var fileSystem = new FileSystem(pwd);

        var tokenDefinitionRegistry = TokenDefinitionRegistry.Instance;
        AssemblerOptions buildOptions = new AssemblerOptions(tokenDefinitionRegistry, OperationType.Build);
        buildOptions.AddRootPath(new RankedString(SourceType.BuildFile, pwd));

        // optional ... specify the output path (i.e., pretend that it was specified via -Output). 

        var commentRemovalDirectives = CommentRemovalDirectives.RemoveHeaderComments | CommentRemovalDirectives.RemoveDocComments;
        buildOptions.SetDirectives(LineEndingsType.CrLf, commentRemovalDirectives, tokenExclusionDirectives: TokenExclusionDirectives.ExcludeBlockComments);

        BuildResult buildResult = new BuildResult();

        var opsFactory = new OpsFactory(fileSystem, buildOptions);

        Assembler assembler = new Assembler(opsFactory, buildOptions, buildResult);
        assembler.Assemble(buildFile);
    }

    [Test]
    public void Execute_S4_Test_Build()
    {
        // SIMULATE roughly what PowerShell would do via a SIMPLE run. 
        // MOSTLY so'z I can get in here and debug things ... vs looking at the screen and wondering what something MIGHT do under build cuz I can't debug C# from powershell. 

        string pwd = "D:\\Dropbox\\Repositories\\tsmake\\test_files";
        string buildFile = "D:\\Dropbox\\Repositories\\tsmake\\test_files\\test.build.sql";
        var fileSystem = new FileSystem(pwd);

        var tokenDefinitionRegistry = TokenDefinitionRegistry.Instance;
        AssemblerOptions buildOptions = new AssemblerOptions(tokenDefinitionRegistry, OperationType.Build);
        buildOptions.AddRootPath(new RankedString(SourceType.BuildFile, pwd));

        // optional ... specify the output path (i.e., pretend that it was specified via -Output). 
        
        var commentRemovalDirectives = CommentRemovalDirectives.RemoveHeaderComments | CommentRemovalDirectives.RemoveDocComments;
        buildOptions.SetDirectives(LineEndingsType.CrLf, commentRemovalDirectives, tokenExclusionDirectives: TokenExclusionDirectives.ExcludeBlockComments);

        BuildResult buildResult = new BuildResult();

        var opsFactory = new OpsFactory(fileSystem, buildOptions);

        Assembler assembler = new Assembler(opsFactory, buildOptions, buildResult);
        assembler.Assemble(buildFile);
    }
}