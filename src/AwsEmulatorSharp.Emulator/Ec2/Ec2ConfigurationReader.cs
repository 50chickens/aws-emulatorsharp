using System.Text.Json;
using System.Text.Json.Serialization;

namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2ConfigurationReader
{
    private const string ConfigurationFolderName = "config";
    private const string ConfigurationFileName = "ec2.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
    };

    public Ec2Configuration Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<Ec2Configuration>(json, _serializerOptions);

        return configuration!;
    }
}
