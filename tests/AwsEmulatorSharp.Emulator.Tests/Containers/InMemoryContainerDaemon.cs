using System.Globalization;
using AwsEmulatorSharp.Emulator.Containers;

namespace AwsEmulatorSharp.Emulator.Tests;

internal class InMemoryContainerDaemon : IContainerDaemon
{
    private const string AddressFormat = "10.0.0.{0}";

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _labelsByContainerId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _addressByContainerId = new(StringComparer.Ordinal);
    private readonly ContainerCommandResult _commandResult;
    private int _containerCount;

    internal InMemoryContainerDaemon(ContainerCommandResult commandResult)
    {
        _commandResult = commandResult;
        LastStartedImageReference = string.Empty;
    }

    internal string LastStartedImageReference { get; private set; }

    public string StartContainer(string imageReference, IReadOnlyDictionary<string, string> labels)
    {
        _containerCount++;
        var containerId = $"container-{_containerCount}";
        var address = string.Format(CultureInfo.InvariantCulture, AddressFormat, _containerCount);
        _labelsByContainerId.Add(containerId, labels);
        _addressByContainerId.Add(containerId, address);
        LastStartedImageReference = imageReference;
        return containerId;
    }

    public bool IsRunning(string containerId)
    {
        var isPresent = _labelsByContainerId.ContainsKey(containerId);
        return isPresent;
    }

    public string GetAddress(string containerId)
    {
        var address = _addressByContainerId[containerId];
        return address;
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
        _addressByContainerId.Remove(containerId);
    }
}
