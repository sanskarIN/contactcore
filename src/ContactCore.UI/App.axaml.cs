using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ContactCore.UI;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = AppBootstrapper.CreateServices();
        RequestedThemeVariant = ResolveTheme(services.Preferences.Theme);

        var viewModel = new MainViewModel(services)
        {
            ThemeChangeRequested = theme => RequestedThemeVariant = ResolveTheme(theme)
        };
        var mainView = new MainView { DataContext = viewModel };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window
            {
                Title = "ContactCore",
                Width = 1120,
                Height = 760,
                MinWidth = 720,
                MinHeight = 520,
                Content = mainView
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = mainView;
        }

        _ = viewModel.InitializeAsync();
        base.OnFrameworkInitializationCompleted();
    }

    private static ThemeVariant ResolveTheme(string? theme) => theme?.Trim().ToLowerInvariant() switch
    {
        "light" => ThemeVariant.Light,
        "dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };
}
