namespace AwsEmulatorSharp.Emulator.Ec2;

public class Ec2KeyPair
{
    public required string Name { get; set; }

    public required string Fingerprint { get; set; }

    public required string KeyType { get; set; }
}
