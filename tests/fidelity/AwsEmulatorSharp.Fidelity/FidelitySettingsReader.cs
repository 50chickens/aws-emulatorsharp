using System.Text.Json;

namespace AwsEmulatorSharp.Fidelity;

public class FidelitySettingsReader
{
    private const string SettingsFileName = "fidelity.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FidelitySettings Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<FidelitySettings>(json, _serializerOptions);

        return settings!;
    }
}
