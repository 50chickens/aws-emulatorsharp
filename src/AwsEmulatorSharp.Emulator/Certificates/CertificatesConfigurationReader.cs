using System.Text.Json;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificatesConfigurationReader
{
    private const string ConfigurationFolderName = "config";
    private const string ConfigurationFileName = "certificates.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CertificatesConfiguration Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFolderName, ConfigurationFileName);
        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<CertificatesConfiguration>(json, _serializerOptions);

        return configuration!;
    }
}
