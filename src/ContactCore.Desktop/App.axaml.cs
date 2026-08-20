using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ContactCore.Application;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = new AppPaths(Environment.GetEnvironmentVariable("CONTACTCORE_DATA_PATH"));
            var preferences = new JsonAppPreferences(paths.SettingsPath);
            var factory = new SqliteConnectionFactory(paths.DatabasePath, () => preferences.DatabaseKey);
            var migrator = new DatabaseMigrator(factory);
            var repository = new SqliteContactRepository(factory, migrator);
            var service = new ContactService(repository);
            var backup = new BackupService(paths, factory, migrator);
            var vm = new MainWindowViewModel(service, backup, preferences, paths);
            desktop.MainWindow = new MainWindow { DataContext = vm };
            _ = vm.InitializeAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
