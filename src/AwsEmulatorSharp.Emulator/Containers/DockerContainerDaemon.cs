using System.Diagnostics;
using System.Globalization;

namespace AwsEmulatorSharp.Emulator.Containers;

public class DockerContainerDaemon : IContainerDaemon
{
    private const string ExecutableName = "docker";
    private const string RunVerb = "run";
    private const string DetachFlag = "--detach";
    private const string LabelFlag = "--label=";
    private const string InspectVerb = "inspect";
    private const string FormatFlag = "--format=";
    private const string StateFormat = "{{.State.Running}}";
    private const string AddressFormat = "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}";
    private const string RunningValue = "true";
    private const string ListVerb = "ps";
    private const string AllFlag = "--all";
    private const string QuietFlag = "--quiet";
    private const string FilterFlag = "--filter=";
    private const string LabelFilterName = "label=";
    private const string ExecVerb = "exec";
    private const string SetEnvironmentStatementFormat = "[Environment]::SetEnvironmentVariable('{0}','{1}','Machine')";
    private const string StatementSeparator = ";";
    private const string StopVerb = "stop";
    private const string StartVerb = "start";
    private const string RestartVerb = "restart";
    private const string RemoveVerb = "rm";
    private const string ForceFlag = "--force";
    private const string KeyValueSeparator = "=";
    private const string LineSeparator = "\n";
    private const string FailureMessageFormat = "docker exited with {0}: {1}";

    public string StartContainer(string imageReference, IReadOnlyDictionary<string, string> labels)
    {
        var arguments = new List<string> { RunVerb, DetachFlag };
        var labelArguments = labels
            .Select(pair => $"{LabelFlag}{pair.Key}{KeyValueSeparator}{pair.Value}")
            .ToList();
        arguments.AddRange(labelArguments);
        arguments.Add(imageReference);

        var result = RunDocker(arguments);
        ThrowOnFailure(result);

        var containerId = result.StandardOutput.Trim();
        return containerId;
    }

    public bool IsRunning(string containerId)
    {
        var arguments = new List<string> { InspectVerb, $"{FormatFlag}{StateFormat}", containerId };
        var result = RunDocker(arguments);

        var stateValue = result.StandardOutput.Trim();
        var isRunning = string.Equals(stateValue, RunningValue, StringComparison.Ordinal);
        return isRunning;
    }

    public void StopContainer(string containerId)
    {
        var arguments = new List<string> { StopVerb, containerId };
        var result = RunDocker(arguments);
        ThrowOnFailure(result);
    }

    public void StartExistingContainer(string containerId)
    {
        var arguments = new List<string> { StartVerb, containerId };
        var result = RunDocker(arguments);
        ThrowOnFailure(result);
    }

    public void RestartContainer(string containerId)
    {
        var arguments = new List<string> { RestartVerb, containerId };
        var result = RunDocker(arguments);
        ThrowOnFailure(result);
    }

    public string GetAddress(string containerId)
    {
        var arguments = new List<string> { InspectVerb, $"{FormatFlag}{AddressFormat}", containerId };
        var result = RunDocker(arguments);
        ThrowOnFailure(result);

        var address = result.StandardOutput.Trim();
        return address;
    }

    public IReadOnlyList<string> FindContainerIds(string labelKey, string labelValue)
    {
        var filter = $"{LabelFilterName}{labelKey}{KeyValueSeparator}{labelValue}";
        var arguments = new List<string> { ListVerb, AllFlag, QuietFlag, $"{FilterFlag}{filter}" };
        var result = RunDocker(arguments);

        List<string> containerIds = result.StandardOutput
            .Split(LineSeparator, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        return containerIds;
    }

    public ContainerCommandResult RunCommand(string containerId, IReadOnlyList<string> command)
    {
        var arguments = new List<string> { ExecVerb, containerId };
        arguments.AddRange(command);

        var result = RunDocker(arguments);
        return result;
    }

    public void SetEnvironment(string containerId, IReadOnlyDictionary<string, string> environment)
    {
        var statements = environment
            .Select(pair => string.Format(CultureInfo.InvariantCulture, SetEnvironmentStatementFormat, pair.Key, pair.Value))
            .ToList();
        var script = string.Join(StatementSeparator, statements);

        var command = script.ToPowerShellCommand();
        var result = RunCommand(containerId, command);
        ThrowOnFailure(result);
    }

    public void RemoveContainer(string containerId)
    {
        var arguments = new List<string> { RemoveVerb, ForceFlag, containerId };
        RunDocker(arguments);
    }

    private ContainerCommandResult RunDocker(List<string> arguments)
    {
        var startInfo = new ProcessStartInfo(ExecutableName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        arguments.ForEach(startInfo.ArgumentList.Add);

        using var process = Process.Start(startInfo)!;
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var standardError = standardErrorTask.GetAwaiter().GetResult();

        var result = new ContainerCommandResult(standardOutput, standardError, process.ExitCode);
        return result;
    }

    private void ThrowOnFailure(ContainerCommandResult result)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        var message = string.Format(CultureInfo.InvariantCulture, FailureMessageFormat, result.ExitCode, result.StandardError.Trim());
        throw new InvalidOperationException(message);
    }
}
