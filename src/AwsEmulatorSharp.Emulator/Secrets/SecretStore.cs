using System.Collections.Concurrent;

namespace AwsEmulatorSharp.Emulator.Secrets;

public class SecretStore
{
    private readonly ConcurrentDictionary<string, SecretValue> _secretValues = new(StringComparer.Ordinal);

    public SecretValue PutSecretValue(string secretName, string secretString)
    {
        var versionId = Guid.NewGuid().ToString();
        var createdDate = DateTimeOffset.UtcNow;
        var secretValue = new SecretValue(secretName, secretString, versionId, createdDate);
        _secretValues[secretName] = secretValue;

        return secretValue;
    }

    public IReadOnlyList<SecretValue> FindSecretValue(string secretName)
    {
        if (!_secretValues.TryGetValue(secretName, out var secretValue))
        {
            var absentSecretValues = new List<SecretValue>();

            return absentSecretValues;
        }

        var foundSecretValues = new List<SecretValue> { secretValue };

        return foundSecretValues;
    }
}
