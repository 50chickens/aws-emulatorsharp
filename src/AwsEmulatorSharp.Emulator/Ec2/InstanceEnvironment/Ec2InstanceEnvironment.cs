using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Identity;

namespace AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;

public class Ec2InstanceEnvironment
{
    private readonly IdentityConfiguration _identityConfiguration;
    private readonly HostConfiguration _hostConfiguration;

    public Ec2InstanceEnvironment(IdentityConfiguration identityConfiguration, HostConfiguration hostConfiguration)
    {
        _identityConfiguration = identityConfiguration;
        _hostConfiguration = hostConfiguration;
    }

    public IReadOnlyDictionary<string, string> For(Ec2Instance instance, string regionName, string instanceProfileName)
    {
        var environment = instance.ToEnvironment(regionName);
        var instanceProfiles = _identityConfiguration.InstanceProfiles;
        if (!instanceProfiles.ContainsKey(instanceProfileName))
        {
            return environment;
        }

        var profile = instanceProfiles[instanceProfileName];
        var profileEnvironment = environment.WithInstanceProfile(profile, _hostConfiguration.ListenerPort);

        return profileEnvironment;
    }
}
