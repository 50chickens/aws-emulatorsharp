namespace AwsEmulatorSharp.Emulator.Install;

public class InstallPlan
{
    public InstallPlan(string instanceId, string mediaBucketName, IReadOnlyList<InstallStep> steps)
    {
        InstanceId = instanceId;
        MediaBucketName = mediaBucketName;
        Steps = steps;
    }

    public string InstanceId { get; }

    public string MediaBucketName { get; }

    public IReadOnlyList<InstallStep> Steps { get; }
}
