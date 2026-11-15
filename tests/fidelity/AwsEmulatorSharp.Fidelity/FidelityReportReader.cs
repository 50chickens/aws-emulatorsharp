using System.Text.Json;

namespace AwsEmulatorSharp.Fidelity;

public class FidelityReportReader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FidelityReport Read(string reportPath)
    {
        var json = File.ReadAllText(reportPath);
        var report = JsonSerializer.Deserialize<FidelityReport>(json, _serializerOptions);

        return report!;
    }
}
