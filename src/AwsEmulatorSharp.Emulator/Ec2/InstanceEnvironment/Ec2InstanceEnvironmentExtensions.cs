using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

namespace AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;

internal static class Ec2InstanceEnvironmentExtensions
{
    private const string InstanceIdVariableName = "AWS_EC2_INSTANCE_ID";
    private const string PrivateIpAddressVariableName = "AWS_EC2_PRIVATE_IP";
    private const string RegionVariableName = "AWS_REGION";

    internal static IReadOnlyDictionary<string, string> ToEnvironment(this Ec2Instance instance, string regionName)
    {
        var environment = new Dictionary<string, string>
        {
            [InstanceIdVariableName] = instance.InstanceId,
            [PrivateIpAddressVariableName] = instance.PrivateIpAddress,
            [RegionVariableName] = regionName
        };

        return environment;
    }
}
