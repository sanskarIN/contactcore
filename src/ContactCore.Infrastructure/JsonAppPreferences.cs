using System.Text.Json;
using ContactCore.Application;

namespace ContactCore.Infrastructure;

public sealed class JsonAppPreferences : IAppPreferences
{
    private readonly string _path;
    public JsonAppPreferences(string path)
    {
        _path = path;
        if (!File.Exists(path)) return;
        try
        {
            var model = JsonSerializer.Deserialize<Model>(File.ReadAllText(path));
            if (model is not null) { Theme = model.Theme; ReducedMotion = model.ReducedMotion; ConfirmPermanentDelete = model.ConfirmPermanentDelete; }
        }
        catch (JsonException) { /* corrupted preferences fall back to safe defaults */ }
        DatabaseKey = Environment.GetEnvironmentVariable("CONTACTCORE_DATABASE_KEY");
    }
    public string Theme { get; set; } = "System";
    public bool ReducedMotion { get; set; }
    public bool ConfirmPermanentDelete { get; set; } = true;
    public string? DatabaseKey { get; set; }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(new Model(Theme, ReducedMotion, ConfirmPermanentDelete), new JsonSerializerOptions { WriteIndented = true });
        var tmp = _path + ".tmp"; File.WriteAllText(tmp, json); File.Move(tmp, _path, true);
    }
    private sealed record Model(string Theme = "System", bool ReducedMotion = false, bool ConfirmPermanentDelete = true);
}
