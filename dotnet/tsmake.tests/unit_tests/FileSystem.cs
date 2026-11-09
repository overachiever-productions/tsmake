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

    [TestCase(@"\\nas.company.com\share\tsmake\test_files\common.sql")]
    [TestCase(@"\\192.168.1.10\builds\common.sql")]
    [TestCase(@"\\nas\share")]
    public void It_Correctly_Detects_Absolute_UncShare_Paths(string path)
    {
        var fs = new tsmake.FileSystem(@"C:\temp");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Absolute));
    }

    [TestCase(@"~\test_files\common.sql")]
    [TestCase(@"~\common.sql")]
    [TestCase(@"~\")]
    public void It_Correctly_Detects_Root_Relative_Paths(string path)
    {
        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var pathType = fs.GetPathType(path);

        Assert.That(pathType, Is.EqualTo(PathType.Rooted));
    }

    [TestCase(@"test_files\common.sql")]
    [TestCase(@"common.sql")]
    [TestCase(@"..\common.sql")]
    [TestCase(@"~common.sql")]           // a file-name that merely STARTS with '~' is NOT rooted.
    [TestCase(@"~backups\common.sql")]
    public void It_Correctly_Detects_Relative_Paths(string path)
    {
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
        var path = @"~\common.sql";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\common.sql", translatedPath);
    }

    [Test]
    public void It_Translates_Relative_Directory_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"~\common";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\common", translatedPath);
    }

    [Test]
    public void It_Translates_Relative_Directory_and_File_Paths()
    {
        var root = @"D:\repos\myrepo";
        var path = @"~\test_files\common.sql";

        var fs = new tsmake.FileSystem(root);
        var translatedPath = fs.TranslatePath(path);

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\test_files\common.sql", translatedPath);
    }

    [Test]
    public void It_Translates_Rooted_Paths_Without_Removing_Other_Tildes()
    {
        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var translatedPath = fs.TranslatePath(@"~\backups~old\common~1.sql");

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\backups~old\common~1.sql", translatedPath);
    }

    [Test]
    public void It_Translates_Rooted_Paths_When_Root_Has_A_Trailing_Separator()
    {
        var fs = new tsmake.FileSystem(@"D:\repos\myrepo\");
        var translatedPath = fs.TranslatePath(@"~\common.sql");

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\common.sql", translatedPath);
    }

    [Test]
    public void It_Translates_UncShare_Paths_As_Is()
    {
        var path = @"\\nas.company.com\share\tsmake\common.sql";

        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");
        var translatedPath = fs.TranslatePath(path);

        Assert.That(translatedPath, Is.EqualTo(path));
    }

    [Test]
    public void It_Translates_Plain_Relative_Paths()
    {
        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");

        StringAssert.AreEqualIgnoringCase(@"D:\repos\myrepo\test_files\common.sql", fs.TranslatePath(@"test_files\common.sql"));
        StringAssert.AreEqualIgnoringCase(@"D:\repos\common.sql", fs.TranslatePath(@"..\common.sql"));
    }
    #endregion

    #region Directory Existence Tests (real disk - in a throw-away temp directory)
    private static void WithTempDirectory(Action<string> test)
    {
        string temp = Path.Combine(Path.GetTempPath(), "tsmake_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            test(temp);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public void DirectoryExists_Is_True_For_An_Existing_Directory()
    {
        WithTempDirectory(temp =>
        {
            var fs = new tsmake.FileSystem(temp);

            Assert.That(fs.DirectoryExists(temp), Is.True);
            Assert.That(fs.DirectoryExists(temp + Path.DirectorySeparatorChar), Is.True);
        });
    }

    [Test]
    public void DirectoryExists_Is_False_For_A_Missing_Directory_Even_If_Its_Parent_Exists()
    {
        WithTempDirectory(temp =>
        {
            var fs = new tsmake.FileSystem(temp);

            Assert.That(fs.DirectoryExists(Path.Combine(temp, "no_such_folder")), Is.False);
        });
    }

    [Test]
    public void DirectoryExists_Is_False_For_A_File()
    {
        WithTempDirectory(temp =>
        {
            string file = Path.Combine(temp, "a.sql");
            File.WriteAllText(file, "-- a");
            var fs = new tsmake.FileSystem(temp);

            Assert.That(fs.DirectoryExists(file), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void DirectoryExists_Is_False_For_Empty_Paths(string path)
    {
        var fs = new tsmake.FileSystem(@"D:\repos\myrepo");

        Assert.That(fs.DirectoryExists(path), Is.False);
    }
    #endregion
}