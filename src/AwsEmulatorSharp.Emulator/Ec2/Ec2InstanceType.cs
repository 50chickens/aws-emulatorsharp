namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2InstanceType
{
    public required string Name { get; set; }

    public required int VCpuCount { get; set; }

    public required int MemoryMib { get; set; }

    public required bool CurrentGeneration { get; set; }

    public required bool FreeTierEligible { get; set; }
}
