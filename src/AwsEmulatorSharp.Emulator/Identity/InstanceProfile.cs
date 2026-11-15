namespace AwsEmulatorSharp.Emulator.Identity;

public class InstanceProfile
{
    public required string Arn { get; set; }

    public required string AccessKeyId { get; set; }

    public required string SecretAccessKey { get; set; }
}
