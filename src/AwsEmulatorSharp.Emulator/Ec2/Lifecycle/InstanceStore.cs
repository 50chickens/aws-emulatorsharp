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

    private readonly ConcurrentDictionary<string, Ec2Instance> _instances = new(StringComparer.Ordinal);

    public Ec2Instance Launch(string imageId, string instanceType, string containerId)
    {
        var instanceId = InstanceIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var reservationId = ReservationIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var instance = new Ec2Instance(instanceId, reservationId, imageId, instanceType, containerId, RunningStateName, RunningStateCode);
        _instances[instanceId] = instance;

        return instance;
    }

    public Ec2Instance Stop(Ec2Instance instance)
    {
        var stoppedInstance = instance.WithState(StoppedStateName, StoppedStateCode);
        _instances[instance.InstanceId] = stoppedInstance;

        return stoppedInstance;
    }

    public Ec2Instance Start(Ec2Instance instance)
    {
        var runningInstance = instance.WithState(RunningStateName, RunningStateCode);
        _instances[instance.InstanceId] = runningInstance;

        return runningInstance;
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
