using AwsEmulatorSharp.Emulator.Install;

namespace AwsEmulatorSharp.Emulator.Pipeline;

public class PipelineConfiguration
{
    public required InstallConfiguration Install { get; set; }

    public required string SecretName { get; set; }

    public required string SecretString { get; set; }

    public required string ImageId { get; set; }

    public required string InstanceType { get; set; }

    public required string UserDataScript { get; set; }

    public required TimeSpan AwaitTimeout { get; set; }
}
