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
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ContactCore");
            Directory.CreateDirectory(appData);

            var database = new SqliteDatabase(Path.Combine(appData, "contactcore.db"));
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
}
