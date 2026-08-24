using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace ContactCore.Android;

[Activity(
    Label = "ContactCore",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
