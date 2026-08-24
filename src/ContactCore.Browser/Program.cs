using Avalonia;
using Avalonia.Browser;
using ContactCore.UI;

namespace ContactCore.Browser;

internal static class Program
{
    private static Task Main(string[] args)
    {
        AppBootstrapper.Configure(BrowserAppServices.Create);
        return AppBuilder.Configure<App>().StartBrowserAppAsync("out");
    }
}
