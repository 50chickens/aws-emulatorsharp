namespace AwsEmulatorSharp.Emulator.Identity;

public class IdentityConfiguration
{
    public required string AccessKeyId { get; set; }

    public required string AccountId { get; set; }

    public required string CallerArn { get; set; }

    public required int SessionDurationSeconds { get; set; }

    public required Dictionary<string, InstanceProfile> InstanceProfiles { get; set; }
}
