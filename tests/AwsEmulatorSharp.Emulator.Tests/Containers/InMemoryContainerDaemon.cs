using System.Globalization;
using AwsEmulatorSharp.Emulator.Containers;

namespace AwsEmulatorSharp.Emulator.Tests;

internal class InMemoryContainerDaemon : IContainerDaemon
{
    private const string AddressFormat = "10.0.0.{0}";

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _labelsByContainerId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _addressByContainerId = new(StringComparer.Ordinal);
    private readonly HashSet<string> _runningContainerIds = new(StringComparer.Ordinal);
    private readonly ContainerCommandResult _commandResult;
    private int _containerCount;

    internal InMemoryContainerDaemon(ContainerCommandResult commandResult)
    {
        _commandResult = commandResult;
        LastStartedImageReference = string.Empty;
        LastStoppedContainerId = string.Empty;
        LastStartedExistingContainerId = string.Empty;
        LastRestartedContainerId = string.Empty;
        LastRemovedContainerId = string.Empty;
        LastCommandContainerId = string.Empty;
        LastCommand = Array.Empty<string>();
    }

    internal string LastStartedImageReference { get; private set; }

    internal string LastStoppedContainerId { get; private set; }

    internal string LastStartedExistingContainerId { get; private set; }

    internal string LastRestartedContainerId { get; private set; }

    internal int StartedContainerCount => _containerCount;

    internal string LastRemovedContainerId { get; private set; }

    internal string LastCommandContainerId { get; private set; }

    internal IReadOnlyList<string> LastCommand { get; private set; }

    public string StartContainer(string imageReference, IReadOnlyDictionary<string, string> labels)
    {
        _containerCount++;
        var containerId = $"container-{_containerCount}";
        var address = string.Format(CultureInfo.InvariantCulture, AddressFormat, _containerCount);
        _labelsByContainerId.Add(containerId, labels);
        _addressByContainerId.Add(containerId, address);
        _runningContainerIds.Add(containerId);
        LastStartedImageReference = imageReference;
        return containerId;
    }

    public void StopContainer(string containerId)
    {
        _runningContainerIds.Remove(containerId);
        LastStoppedContainerId = containerId;
    }

    public void StartExistingContainer(string containerId)
    {
        _runningContainerIds.Add(containerId);
        LastStartedExistingContainerId = containerId;
    }

    public void RestartContainer(string containerId)
    {
        _runningContainerIds.Add(containerId);
        LastRestartedContainerId = containerId;
    }

    public bool IsRunning(string containerId)
    {
        var isRunning = _runningContainerIds.Contains(containerId);
        return isRunning;
    }

    public string GetAddress(string containerId)
    {
        var address = _addressByContainerId[containerId];
        return address;
    }

    public ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command)
    {
        LastCommandContainerId = containerId;
        LastCommand = command;
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
        _addressByContainerId.Remove(containerId);
        _runningContainerIds.Remove(containerId);
        LastRemovedContainerId = containerId;
    }
}
