using System.Text.Json;
using ContactCore.Application;
using ContactCore.UI;

namespace ContactCore.Browser;

public static class BrowserAppServices
{
    public static AppServices Create()
    {
        var repository = new BrowserContactRepository();
        var preferences = new BrowserPreferences();
        return new AppServices(
            new ContactService(repository),
            new UnsupportedBrowserBackupService(),
            preferences,
            new AppPlatformCapabilities(
                "Web / WebAssembly",
                "Browser IndexedDB (ContactCore local site storage)",
                "Use CSV or vCard export for portable browser copies",
                SupportsDatabaseBackups: false,
                SupportsDatabaseEncryption: false));
    }
}

internal sealed class BrowserPreferences : IAppPreferences
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BrowserPreferences()
    {
        var json = BrowserStorageInterop.LoadPreferences();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var model = JsonSerializer.Deserialize<Model>(json, JsonOptions);
            if (model is null) return;
            Theme = NormalizeTheme(model.Theme);
            ReducedMotion = model.ReducedMotion;
            ConfirmPermanentDelete = model.ConfirmPermanentDelete;
        }
        catch (JsonException)
        {
            // Preferences are non-critical; keep privacy-preserving safe defaults.
        }
    }

    public string Theme { get; set; } = "System";
    public bool ReducedMotion { get; set; }
    public bool ConfirmPermanentDelete { get; set; } = true;
    public string? DatabaseKey { get; set; }

    public void Save()
    {
        var model = new Model(NormalizeTheme(Theme), ReducedMotion, ConfirmPermanentDelete);
        BrowserStorageInterop.SavePreferences(JsonSerializer.Serialize(model, JsonOptions));
    }

    private static string NormalizeTheme(string? theme) => theme?.Trim().ToLowerInvariant() switch
    {
        "light" => "Light",
        "dark" => "Dark",
        _ => "System"
    };

    private sealed record Model(string Theme, bool ReducedMotion, bool ConfirmPermanentDelete);
}

internal sealed class UnsupportedBrowserBackupService : IBackupService
{
    public Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default) =>
        Task.FromException<string>(new PlatformNotSupportedException(
            "Native database backups are not available in the WebAssembly target. Use CSV or vCard export."));

    public Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default) =>
        Task.FromException(new PlatformNotSupportedException(
            "Native database restore is not available in the WebAssembly target."));
}
