using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ContactCore.Application;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configuredKey = Environment.GetEnvironmentVariable("CONTACTCORE_DATABASE_KEY");
            if (!string.IsNullOrEmpty(configuredKey))
                throw new InvalidOperationException("Database encryption was requested, but no encrypted SQLite provider is configured. ContactCore will not silently open an unencrypted database.");

            var databasePath = ResolveDatabasePath();
            var database = new SqliteDatabase(databasePath);
            database.InitializeAsync().GetAwaiter().GetResult();
            var repository = new SqliteContactRepository(database);
            var contactService = new ContactService(repository);
            var duplicateService = new DuplicateService(repository);
            var backupService = new SqliteBackupService(database);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(contactService, duplicateService, backupService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string ResolveDatabasePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("CONTACTCORE_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (!Path.IsPathRooted(configuredPath))
                throw new InvalidOperationException("CONTACTCORE_DATA_PATH must be an absolute database file path.");
            return Path.GetFullPath(configuredPath);
        }

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ContactCore");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "contactcore.db");
    }
}
