using System.Globalization;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Identity;

namespace AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;

internal static class Ec2InstanceEnvironmentExtensions
{
    private const string InstanceIdVariableName = "AWS_EC2_INSTANCE_ID";
    private const string PrivateIpAddressVariableName = "AWS_EC2_PRIVATE_IP";
    private const string RegionVariableName = "AWS_REGION";
    private const string AccessKeyIdVariableName = "AWS_ACCESS_KEY_ID";
    private const string SecretAccessKeyVariableName = "AWS_SECRET_ACCESS_KEY";
    private const string EndpointUrlVariableName = "AWS_ENDPOINT_URL";
    private const string ContainerHostName = "host.docker.internal";
    private const string EndpointFormat = "http://{0}:{1}";

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

    internal static IReadOnlyDictionary<string, string> WithInstanceProfile(this IReadOnlyDictionary<string, string> environment, InstanceProfile profile, int listenerPort)
    {
        var endpoint = string.Format(CultureInfo.InvariantCulture, EndpointFormat, ContainerHostName, listenerPort);
        var profileEnvironment = new Dictionary<string, string>(environment)
        {
            [AccessKeyIdVariableName] = profile.AccessKeyId,
            [SecretAccessKeyVariableName] = profile.SecretAccessKey,
            [EndpointUrlVariableName] = endpoint
        };

        return profileEnvironment;
    }
}
