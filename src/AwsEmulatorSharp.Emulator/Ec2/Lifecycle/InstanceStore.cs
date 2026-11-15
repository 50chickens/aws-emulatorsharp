using System.Collections.Concurrent;
using System.Security.Cryptography;
using AwsEmulatorSharp.Emulator.Ec2.Describe;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class InstanceStore
{
    private const string InstanceIdPrefix = "i-";
    private const string ReservationIdPrefix = "r-";
    private const int IdentifierLength = 17;
    private const string RunningStateName = "running";
    private const int RunningStateCode = 16;
    private const string StoppedStateName = "stopped";
    private const int StoppedStateCode = 80;
    private const string TerminatedStateName = "terminated";
    private const int TerminatedStateCode = 48;
    private const int FirstBootCount = 1;
    private const bool NoUserDataFailure = false;

    private readonly ConcurrentDictionary<string, Ec2Instance> _instances = new(StringComparer.Ordinal);

    public Ec2Instance Launch(string imageId, string instanceType, string containerId, string privateIpAddress)
    {
        var instanceId = InstanceIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var reservationId = ReservationIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var instance = new Ec2Instance(instanceId, reservationId, imageId, instanceType, containerId, privateIpAddress, RunningStateName, RunningStateCode, FirstBootCount, string.Empty, NoUserDataFailure);
        _instances[instanceId] = instance;

        return instance;
    }

    public Ec2Instance RecordConsoleOutput(Ec2Instance instance, string consoleOutput)
    {
        var recordedInstance = instance.WithConsoleOutput(consoleOutput);
        _instances[instance.InstanceId] = recordedInstance;

        return recordedInstance;
    }

    public Ec2Instance RecordUserDataFailure(Ec2Instance instance)
    {
        var failedInstance = instance.WithUserDataFailure();
        _instances[instance.InstanceId] = failedInstance;

        return failedInstance;
    }

    public string ReadConsoleOutput(string instanceId)
    {
        var consoleOutput = _instances.TryGetValue(instanceId, out var instance) ? instance.ConsoleOutput : string.Empty;

        return consoleOutput;
    }

    public Ec2Instance Stop(Ec2Instance instance)
    {
        var stoppedInstance = instance.WithState(StoppedStateName, StoppedStateCode);
        _instances[instance.InstanceId] = stoppedInstance;

        return stoppedInstance;
    }

    public Ec2Instance Start(Ec2Instance instance)
    {
        var runningInstance = instance.Booted(RunningStateName, RunningStateCode);
        _instances[instance.InstanceId] = runningInstance;

        return runningInstance;
    }

    public Ec2Instance Terminate(Ec2Instance instance)
    {
        var terminatedInstance = instance.WithState(TerminatedStateName, TerminatedStateCode);
        _instances[instance.InstanceId] = terminatedInstance;

        return terminatedInstance;
    }

    public IReadOnlyList<Ec2Instance> Find(List<string> instanceIds)
    {
        var instances = _instances
            .Values
            .ToList()
            .SelectRequested(instanceIds, instance => instance.InstanceId)
            .OrderBy(instance => instance.InstanceId, StringComparer.Ordinal)
            .ToList();

        return instances;
    }
}
