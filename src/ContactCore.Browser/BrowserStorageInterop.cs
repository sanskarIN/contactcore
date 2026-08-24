using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace ContactCore.Browser;

[SupportedOSPlatform("browser")]
internal static partial class BrowserStorageInterop
{
    [JSImport("globalThis.contactcoreStorage.loadContacts")]
    internal static partial Task<string> LoadContactsAsync();

    [JSImport("globalThis.contactcoreStorage.saveContacts")]
    internal static partial Task SaveContactsAsync(string json);

    [JSImport("globalThis.contactcoreStorage.loadPreferences")]
    internal static partial string LoadPreferences();

    [JSImport("globalThis.contactcoreStorage.savePreferences")]
    internal static partial void SavePreferences(string json);
}
