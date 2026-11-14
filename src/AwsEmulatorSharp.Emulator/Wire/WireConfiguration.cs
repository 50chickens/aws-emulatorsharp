namespace AwsEmulatorSharp.Emulator.Wire;

public class WireConfiguration : IWireConfiguration
{
    public string QueryNamespace => "http://ec2.amazonaws.com/doc/2016-11-15/";

    public string RegionName => "us-east-1";

    public string RegionEndpoint => "ec2.us-east-1.amazonaws.com";
}
