namespace tsmake.tests.unit_tests;

[TestFixture]
public class FileSystem
{
    #region Path Type Detection Tests
    [Test]
    public void It_Correctly_Detects_Absolute_Paths()
    {
        var path = @"D:\Dropbox\Repositories\tsmake\test_files\common.sql";

        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Absolute));
    }

    [Test]
    public void It_Correctly_Detects_Absolute_UncShare_Paths()
    {
        var path = @"\\\\nas.company.com\share\tsmake\test_files\common.sql";
        var path2 = @"uNC\\nas.company.com\share\tsmake\test_files\common.sql";

        var fs = new tsmake.FileSystem(@"C:\temp");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Absolute));

        var pathType2 = fs.GetPathType(path2);
        Assert.That(pathType2, Is.EqualTo(PathType.Absolute));
    }

    [Test]
    public void It_Correctly_Detects_Root_Relative_Paths()
    {
        var path = @"\\test_files\common.sql";

        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Rooted));
    }

    [Test]
    public void It_Correctly_Detects_Relative_Paths()
    {
        var path = @"test_files\common.sql";

        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Relative));
    }
    #endregion  

    #region Translation Tests
    [Test]
    public void It_Translates_Absolute_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"D:\Dropbox\Repositories\tsmake\test_files\common.sql";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        Assert.That(translatedPath, Is.EqualTo(path));
    }

    [Test]
    public void It_Translates_Simple_Rooted_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"\\common.sql";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\common.sql", translatedPath);
    }

    [Test]
    public void It_Translates_Relative_Directory_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"\\common";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\common", translatedPath);
    }

    [Test]
    public void It_Translates_Relative_Directory_and_File_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"\\test_files\common.sql";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\test_files\common.sql", translatedPath);
    }
    #endregion
}