namespace AwsEmulatorSharp.Emulator.Containers;

public class ContainerInstance
{
    public ContainerInstance(string containerId, string address, ContainerState state)
    {
        ContainerId = containerId;
        Address = address;
        State = state;
    }

    public string ContainerId { get; }

    public string Address { get; }

    public ContainerState State { get; }
}
