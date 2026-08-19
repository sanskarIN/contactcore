using ContactCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class PreferencesAndLoggingTests
{
    [TestMethod]
    public void Preferences_persist_non_secret_options_but_never_database_key()
    {
        var dir = Path.Combine(Path.GetTempPath(), "contactcore-pref-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "settings.json");
            var preferences = new JsonAppPreferences(path)
            {
                Theme = "Dark",
                ReducedMotion = true,
                ConfirmPermanentDelete = false,
                HasCompletedOnboarding = true,
                DatabaseKey = "do-not-persist-this"
            };
            preferences.Save();

            var json = File.ReadAllText(path);
            StringAssert.Contains(json, "Dark");
            Assert.IsFalse(json.Contains("do-not-persist-this", StringComparison.Ordinal));

            var reloaded = new JsonAppPreferences(path);
            Assert.AreEqual("Dark", reloaded.Theme);
            Assert.IsTrue(reloaded.ReducedMotion);
            Assert.IsFalse(reloaded.ConfirmPermanentDelete);
            Assert.IsTrue(reloaded.HasCompletedOnboarding);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [TestMethod]
    public void Corrupted_preferences_fall_back_to_defaults()
    {
        var dir = Path.Combine(Path.GetTempPath(), "contactcore-pref-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "settings.json");
            File.WriteAllText(path, "{ definitely-not-json");

            var preferences = new JsonAppPreferences(path);

            Assert.AreEqual("System", preferences.Theme);
            Assert.IsTrue(preferences.ConfirmPermanentDelete);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [TestMethod]
    public void Diagnostic_sanitizer_redacts_emails_phone_like_numbers_and_bounds_output()
    {
        var input = "Contact ada@example.test at +91 98765 43210. " + new string('x', 3_000);

        var result = RedactingLog.Sanitize(input);

        Assert.IsFalse(result.Contains("ada@example.test", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("98765", StringComparison.Ordinal));
        Assert.IsTrue(result.Length <= 2_001);
    }
}
