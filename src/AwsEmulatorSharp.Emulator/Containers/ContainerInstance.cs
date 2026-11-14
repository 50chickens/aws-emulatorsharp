namespace AwsEmulatorSharp.Emulator.Containers;

public class ContainerInstance
{
    public ContainerInstance(string containerId, ContainerState state)
    {
        ContainerId = containerId;
        State = state;
    }

    public string ContainerId { get; }

    public ContainerState State { get; }
}
