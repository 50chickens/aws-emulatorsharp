namespace AwsEmulatorSharp.Emulator.Containers;

public class ContainersSurface
{
    private const string PowerShellExecutable = "powershell.exe";
    private const string NoProfileFlag = "-NoProfile";
    private const string ExecutionPolicyFlag = "-ExecutionPolicy";
    private const string BypassExecutionPolicy = "Bypass";
    private const string CommandFlag = "-Command";

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

    public void Restart(string containerId)
    {
        _daemon.RestartContainer(containerId);
    }

    public ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command)
    {
        var commandResult = _daemon.RunCommand(containerId, command);
        return commandResult;
    }

    public void RunPowerShellScript(string containerId, string script)
    {
        var command = new List<string>
        {
            PowerShellExecutable,
            NoProfileFlag,
            ExecutionPolicyFlag,
            BypassExecutionPolicy,
            CommandFlag,
            script
        };
        _daemon.RunCommand(containerId, command);
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

    public bool IsRunning(string containerId)
    {
        var isRunning = _daemon.IsRunning(containerId);
        return isRunning;
    }

    private ContainerInstance ToContainerInstance(string containerId)
    {
        var isRunning = _daemon.IsRunning(containerId);
        var state = isRunning ? ContainerState.Running : ContainerState.Stopped;
        var address = _daemon.GetAddress(containerId);
        return new ContainerInstance(containerId, address, state);
    }
}
