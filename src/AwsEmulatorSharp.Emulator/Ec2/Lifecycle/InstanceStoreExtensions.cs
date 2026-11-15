using AwsEmulatorSharp.Emulator.Containers;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

internal static class InstanceStoreExtensions
{
    internal static Ec2Instance Reboot(this InstanceStore store, ContainersSurface containers, Ec2Instance instance)
    {
        containers.Restart(instance.ContainerId);
        var rebootedInstance = store.Start(instance);

        return rebootedInstance;
    }
}
