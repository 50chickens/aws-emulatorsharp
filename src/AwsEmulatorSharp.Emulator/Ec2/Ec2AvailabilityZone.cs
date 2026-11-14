namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2AvailabilityZone
{
    public required string Name { get; set; }

    public required string State { get; set; }

    public required string RegionName { get; set; }
}
