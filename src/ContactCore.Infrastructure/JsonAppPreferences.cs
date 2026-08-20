using System.Text.Json;
using ContactCore.Application;

namespace ContactCore.Infrastructure;

public sealed class JsonAppPreferences : IAppPreferences
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonAppPreferences(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        DatabaseKey = Environment.GetEnvironmentVariable("CONTACTCORE_DATABASE_KEY");

        if (!File.Exists(_path)) return;
        try
        {
            var model = JsonSerializer.Deserialize<Model>(File.ReadAllText(_path), SerializerOptions);
            if (model is not null)
            {
                Theme = NormalizeTheme(model.Theme);
                ReducedMotion = model.ReducedMotion;
                ConfirmPermanentDelete = model.ConfirmPermanentDelete;
            }
        }
        catch (JsonException)
        {
            // Corrupted preferences fall back to safe defaults. DatabaseKey remains runtime-only.
        }
    }

    public string Theme { get; set; } = "System";
    public bool ReducedMotion { get; set; }
    public bool ConfirmPermanentDelete { get; set; } = true;
    public string? DatabaseKey { get; set; }

    public void Save()
    {
        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("The preferences path does not have a parent directory.");
        Directory.CreateDirectory(directory);

        var model = new Model(NormalizeTheme(Theme), ReducedMotion, ConfirmPermanentDelete);
        var json = JsonSerializer.Serialize(model, SerializerOptions);
        var tmp = _path + ".tmp";
        try
        {
            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    private static string NormalizeTheme(string? theme) => theme?.Trim().ToLowerInvariant() switch
    {
        "light" => "Light",
        "dark" => "Dark",
        _ => "System"
    };

    private sealed record Model(
        string? Theme = "System",
        bool ReducedMotion = false,
        bool ConfirmPermanentDelete = true);
}
