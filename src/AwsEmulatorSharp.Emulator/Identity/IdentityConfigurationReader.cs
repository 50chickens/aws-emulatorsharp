using System.Text.Json;

namespace AwsEmulatorSharp.Emulator.Identity;

public class IdentityConfigurationReader
{
    private const string ConfigurationFolderName = "config";
    private const string ConfigurationFileName = "identity.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IdentityConfiguration Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<IdentityConfiguration>(json, _serializerOptions);

        return configuration!;
    }
}
