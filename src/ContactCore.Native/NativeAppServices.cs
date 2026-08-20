using ContactCore.Application;
using ContactCore.Infrastructure;
using ContactCore.UI;

namespace ContactCore.Native;

public static class NativeAppServiceFactory
{
    public static AppServices Create(string platformName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformName);

        var paths = new AppPaths(Environment.GetEnvironmentVariable("CONTACTCORE_DATA_PATH"));
        var preferences = new JsonAppPreferences(paths.SettingsPath);
        var factory = new SqliteConnectionFactory(paths.DatabasePath, () => preferences.DatabaseKey);
        var repository = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
        var service = new ContactService(repository);
        var backup = new BackupService(paths, factory);

        return new AppServices(
            service,
            backup,
            preferences,
            new AppPlatformCapabilities(
                platformName,
                paths.DataDirectory,
                paths.BackupDirectory,
                SupportsDatabaseBackups: true,
                SupportsDatabaseEncryption: true));
    }
}
