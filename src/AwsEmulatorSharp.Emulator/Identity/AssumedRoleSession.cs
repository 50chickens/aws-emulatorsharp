namespace AwsEmulatorSharp.Emulator.Identity;

public class AssumedRoleSession
{
    public AssumedRoleSession(string accessKeyId, string secretAccessKey, string sessionToken, string assumedRoleArn, DateTimeOffset expiration)
    {
        AccessKeyId = accessKeyId;
        SecretAccessKey = secretAccessKey;
        SessionToken = sessionToken;
        AssumedRoleArn = assumedRoleArn;
        Expiration = expiration;
    }

    public string AccessKeyId { get; }

    public string SecretAccessKey { get; }

    public string SessionToken { get; }

    public string AssumedRoleArn { get; }

    public DateTimeOffset Expiration { get; }
}
