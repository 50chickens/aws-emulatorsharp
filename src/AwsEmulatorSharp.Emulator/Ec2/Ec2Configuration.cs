namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2Configuration
{
    public required Dictionary<string, string> AmiToImage { get; set; }

    public required Ec2Region Region { get; set; }

    public required List<Ec2AvailabilityZone> AvailabilityZones { get; set; }

    public required List<Ec2InstanceType> InstanceTypes { get; set; }

    public required List<Ec2KeyPair> KeyPairs { get; set; }

    public required List<Ec2Subnet> Subnets { get; set; }

    public required List<Ec2SecurityGroup> SecurityGroups { get; set; }
}
