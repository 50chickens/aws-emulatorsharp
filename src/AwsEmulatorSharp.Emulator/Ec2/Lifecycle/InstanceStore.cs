using System.Collections.Concurrent;
using System.Security.Cryptography;
using AwsEmulatorSharp.Emulator.Ec2.Describe;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class InstanceStore
{
    private const string InstanceIdPrefix = "i-";
    private const string ReservationIdPrefix = "r-";
    private const int IdentifierLength = 17;
    private const string PendingStateName = "pending";
    private const int PendingStateCode = 0;

    private readonly ConcurrentDictionary<string, Ec2Instance> _instances = new(StringComparer.Ordinal);

    public Ec2Instance Launch(string imageId, string instanceType)
    {
        var instanceId = InstanceIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var reservationId = ReservationIdPrefix + RandomNumberGenerator.GetHexString(IdentifierLength, true);
        var instance = new Ec2Instance(instanceId, reservationId, imageId, instanceType, PendingStateName, PendingStateCode);
        _instances[instanceId] = instance;

        return instance;
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
