using AwsEmulatorSharp.Emulator.Containers;

namespace AwsEmulatorSharp.Emulator.Tests;

internal class InMemoryContainerDaemon : IContainerDaemon
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _labelsByContainerId = new(StringComparer.Ordinal);
    private readonly ContainerCommandResult _commandResult;
    private int _containerCount;

    internal InMemoryContainerDaemon(ContainerCommandResult commandResult)
    {
        _commandResult = commandResult;
    }

    public string StartContainer(string imageReference, IReadOnlyDictionary<string, string> labels)
    {
        _containerCount++;
        var containerId = $"container-{_containerCount}";
        _labelsByContainerId.Add(containerId, labels);
        return containerId;
    }

    public bool IsRunning(string containerId)
    {
        var isPresent = _labelsByContainerId.ContainsKey(containerId);
        return isPresent;
    }

    public ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command)
    {
        return _commandResult;
    }

    public IReadOnlyList<string> FindContainerIds(string labelKey, string labelValue)
    {
        var containerIds = _labelsByContainerId
            .Where(pair => pair.Value.HasLabel(labelKey, labelValue))
            .Select(pair => pair.Key)
            .ToList();
        return containerIds;
    }

    public void RemoveContainer(string containerId)
    {
        _labelsByContainerId.Remove(containerId);
    }
}
