namespace AwsEmulatorSharp.Emulator.Secrets;

public class SecretValue
{
    public SecretValue(string name, string secretString, string versionId, DateTimeOffset createdDate)
    {
        Name = name;
        SecretString = secretString;
        VersionId = versionId;
        CreatedDate = createdDate;
    }

    public string Name { get; }

    public string SecretString { get; }

    public string VersionId { get; }

    public DateTimeOffset CreatedDate { get; }
}
