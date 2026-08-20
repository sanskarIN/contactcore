using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class AppPathsTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "contactcore-path-tests", Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public void Explicit_data_directory_is_normalized_created_and_used_for_all_derived_paths()
    {
        var requested = Path.Combine(_root, "nested", "..", "profile");

        var paths = new AppPaths(requested);
        var expectedDirectory = Path.GetFullPath(requested);

        Assert.AreEqual(expectedDirectory, paths.DataDirectory);
        Assert.IsTrue(Directory.Exists(expectedDirectory));
        Assert.AreEqual(Path.Combine(expectedDirectory, "contactcore.db"), paths.DatabasePath);
        Assert.AreEqual(Path.Combine(expectedDirectory, "settings.json"), paths.SettingsPath);
        Assert.AreEqual(Path.Combine(expectedDirectory, "backups"), paths.BackupDirectory);
    }
}
