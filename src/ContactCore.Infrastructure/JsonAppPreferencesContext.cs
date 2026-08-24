using System.Text.Json.Serialization;

namespace ContactCore.Infrastructure;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonAppPreferencesModel))]
internal sealed partial class JsonAppPreferencesContext : JsonSerializerContext;

internal sealed record JsonAppPreferencesModel(
    string? Theme = "System",
    bool ReducedMotion = false,
    bool ConfirmPermanentDelete = true);
