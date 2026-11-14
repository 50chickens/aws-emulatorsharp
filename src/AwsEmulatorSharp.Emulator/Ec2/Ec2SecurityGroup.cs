namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2SecurityGroup
{
    public required string GroupId { get; set; }

    public required string GroupName { get; set; }

    public required string Description { get; set; }

    public required string VpcId { get; set; }
}
