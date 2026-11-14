namespace AwsEmulatorSharp.Emulator.Wire;

public interface IWireConfiguration
{
    string QueryNamespace { get; }

    string RegionName { get; }

    string RegionEndpoint { get; }
}
