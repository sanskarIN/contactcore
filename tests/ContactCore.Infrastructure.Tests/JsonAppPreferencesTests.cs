using System.Text.Json;
using ContactCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class JsonAppPreferencesTests
{
    private string _directory = null!;
    private string _settingsPath = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "contactcore-preferences-tests", Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(_directory, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public void Save_never_persists_database_key()
    {
        var preferences = new JsonAppPreferences(_settingsPath)
        {
            Theme = "Dark",
            ReducedMotion = true,
            DatabaseKey = "test-only-secret"
        };

        preferences.Save();
        var json = File.ReadAllText(_settingsPath);

        Assert.IsFalse(json.Contains("test-only-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("DatabaseKey", StringComparison.OrdinalIgnoreCase));
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual("Dark", document.RootElement.GetProperty("Theme").GetString());
        Assert.IsTrue(document.RootElement.GetProperty("ReducedMotion").GetBoolean());
    }

    [TestMethod]
    public void Corrupted_preferences_keep_safe_defaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_settingsPath, "{ definitely-not-json");

        var preferences = new JsonAppPreferences(_settingsPath);

        Assert.AreEqual("System", preferences.Theme);
        Assert.IsFalse(preferences.ReducedMotion);
        Assert.IsTrue(preferences.ConfirmPermanentDelete);
    }

    [TestMethod]
    public void Save_normalizes_unknown_theme_to_system()
    {
        var preferences = new JsonAppPreferences(_settingsPath) { Theme = "unsupported" };

        preferences.Save();
        var reloaded = new JsonAppPreferences(_settingsPath);

        Assert.AreEqual("System", reloaded.Theme);
    }
}
