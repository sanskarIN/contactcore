using Avalonia;
using Avalonia.iOS;
using ContactCore.Native;
using ContactCore.UI;
using Foundation;

namespace ContactCore.iOS;

[Register("AppDelegate")]
#pragma warning disable CA1711
public sealed class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        AppBootstrapper.Configure(() => NativeAppServiceFactory.Create("iOS / iPadOS"));
        return base.CustomizeAppBuilder(builder);
    }
}
