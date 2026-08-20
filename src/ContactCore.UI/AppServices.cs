using ContactCore.Application;

namespace ContactCore.UI;

public sealed record AppPlatformCapabilities(
    string PlatformName,
    string DataLocation,
    string BackupLocation,
    bool SupportsDatabaseBackups,
    bool SupportsDatabaseEncryption);

public sealed record AppServices(
    ContactService ContactService,
    IBackupService BackupService,
    IAppPreferences Preferences,
    AppPlatformCapabilities Capabilities);

public static class AppBootstrapper
{
    private static Func<AppServices>? _factory;

    public static void Configure(Func<AppServices> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    internal static AppServices CreateServices() =>
        _factory?.Invoke() ?? throw new InvalidOperationException(
            "ContactCore platform services were not configured before the Avalonia application started.");
}
