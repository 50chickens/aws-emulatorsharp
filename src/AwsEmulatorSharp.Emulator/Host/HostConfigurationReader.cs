using System.Text.Json;

namespace AwsEmulatorSharp.Emulator.Host;

public class HostConfigurationReader
{
    private const string ConfigurationFolderName = "config";
    private const string ConfigurationFileName = "host.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HostConfiguration Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<HostConfiguration>(json, _serializerOptions);

        return configuration!;
    }
}
