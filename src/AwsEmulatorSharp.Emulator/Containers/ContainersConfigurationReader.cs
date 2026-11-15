using System.Text.Json;

namespace AwsEmulatorSharp.Emulator.Containers;

public class ContainersConfigurationReader
{
    private const string ConfigurationFolderName = "config";
    private const string ConfigurationFileName = "containers.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ContainersConfiguration Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<ContainersConfiguration>(json, _serializerOptions);

        return configuration!;
    }
}
