namespace AwsEmulatorSharp.Emulator.Install;

public class InstallPlan
{
    public InstallPlan(string instanceId, string mediaBucketName, string mediaKey, string mediaUrl, string setupCommandLine)
    {
        InstanceId = instanceId;
        MediaBucketName = mediaBucketName;
        MediaKey = mediaKey;
        MediaUrl = mediaUrl;
        SetupCommandLine = setupCommandLine;
    }

    public string InstanceId { get; }

    public string MediaBucketName { get; }

    public string MediaKey { get; }

    public string MediaUrl { get; }

    public string SetupCommandLine { get; }
}
