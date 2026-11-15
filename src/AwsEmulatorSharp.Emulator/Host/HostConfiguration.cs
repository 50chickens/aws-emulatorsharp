namespace AwsEmulatorSharp.Emulator.Host;

public class HostConfiguration
{
    public required int ListenerPort { get; set; }

    public required string ContainerHostName { get; set; }

    public required HostCredential Credentials { get; set; }
}
