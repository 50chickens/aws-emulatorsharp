namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2Subnet
{
    public required string SubnetId { get; set; }

    public required string CidrBlock { get; set; }

    public required string AvailabilityZone { get; set; }

    public required string VpcId { get; set; }

    public required bool DefaultForAz { get; set; }
}
