using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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
            RequestedThemeVariant = ResolveTheme(preferences.Theme);

            var factory = new SqliteConnectionFactory(paths.DatabasePath, () => preferences.DatabaseKey);
            var repository = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
            var service = new ContactService(repository);
            var backup = new BackupService(paths, factory);
            var vm = new MainWindowViewModel(service, backup, preferences, paths)
            {
                ThemeChangeRequested = theme => RequestedThemeVariant = ResolveTheme(theme)
            };

            desktop.MainWindow = new MainWindow { DataContext = vm };
            _ = vm.InitializeAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static ThemeVariant ResolveTheme(string? theme) => theme?.Trim().ToLowerInvariant() switch
    {
        "light" => ThemeVariant.Light,
        "dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };
}
