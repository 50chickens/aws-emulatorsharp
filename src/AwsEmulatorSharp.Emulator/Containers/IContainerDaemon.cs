namespace AwsEmulatorSharp.Emulator.Containers;

public interface IContainerDaemon
{
    string StartContainer(string imageReference, IReadOnlyDictionary<string, string> labels);

    void SetEnvironment(string containerId, IReadOnlyDictionary<string, string> environment);

    void StopContainer(string containerId);

    void StartExistingContainer(string containerId);

    void RestartContainer(string containerId);

    bool IsRunning(string containerId);

    string GetAddress(string containerId);

    ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command);

    IReadOnlyList<string> FindContainerIds(string labelKey, string labelValue);

    void RemoveContainer(string containerId);
}
