namespace AwsEmulatorSharp.Emulator.Install;

public class InstallConfiguration
{
    public required string MediaBucketName { get; set; }

    public required string MediaKeyPrefix { get; set; }

    public required string MediaFolderPath { get; set; }
}
