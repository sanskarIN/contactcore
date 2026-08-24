using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using ContactCore.Native;
using ContactCore.UI;

namespace ContactCore.Android;

[Application]
public sealed class Application : AvaloniaAndroidApplication<App>
{
    public Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        AppBootstrapper.Configure(() => NativeAppServiceFactory.Create("Android"));
        return base.CustomizeAppBuilder(builder);
    }
}
