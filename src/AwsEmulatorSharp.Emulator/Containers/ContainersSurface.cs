namespace AwsEmulatorSharp.Emulator.Containers;

public class ContainersSurface
{
    private readonly IContainerDaemon _daemon;

    public ContainersSurface(IContainerDaemon daemon)
    {
        _daemon = daemon;
    }

    public ContainerInstance Start(string imageReference, IReadOnlyDictionary<string, string> labels)
    {
        var containerId = _daemon.StartContainer(imageReference, labels);
        var container = ToContainerInstance(containerId);
        return container;
    }

    public void Stop(string containerId)
    {
        _daemon.StopContainer(containerId);
    }

    public void StartExisting(string containerId)
    {
        _daemon.StartExistingContainer(containerId);
    }

    public ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command)
    {
        var commandResult = _daemon.RunCommand(containerId, command);
        return commandResult;
    }

    public string Address(string containerId)
    {
        var address = _daemon.GetAddress(containerId);
        return address;
    }

    public IReadOnlyList<ContainerInstance> FindByLabel(string labelKey, string labelValue)
    {
        var containerIds = _daemon.FindContainerIds(labelKey, labelValue);
        var containers = containerIds.Select(ToContainerInstance).ToList();
        return containers;
    }

    public void Remove(string containerId)
    {
        _daemon.RemoveContainer(containerId);
    }

    private ContainerInstance ToContainerInstance(string containerId)
    {
        var isRunning = _daemon.IsRunning(containerId);
        var state = isRunning ? ContainerState.Running : ContainerState.Stopped;
        var address = _daemon.GetAddress(containerId);
        return new ContainerInstance(containerId, address, state);
    }
}
