namespace ContactCore.Infrastructure;

public sealed class AppPaths
{
    public AppPaths(string? overrideDataPath = null)
    {
        DataDirectory = string.IsNullOrWhiteSpace(overrideDataPath) ? GetDefaultDataDirectory() : Path.GetFullPath(overrideDataPath);
        Directory.CreateDirectory(DataDirectory);
        DatabasePath = Path.Combine(DataDirectory, "contactcore.db");
        SettingsPath = Path.Combine(DataDirectory, "settings.json");
        BackupDirectory = Path.Combine(DataDirectory, "backups");
    }

    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string SettingsPath { get; }
    public string BackupDirectory { get; }

    private static string GetDefaultDataDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root)) root = AppContext.BaseDirectory;
        return Path.Combine(root, "ContactCore");
    }
}
