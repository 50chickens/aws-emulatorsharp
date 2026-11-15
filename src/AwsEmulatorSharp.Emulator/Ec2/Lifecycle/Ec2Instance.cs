namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class Ec2Instance
{
    public Ec2Instance(string instanceId, string reservationId, string imageId, string instanceType, string containerId, string privateIpAddress, string stateName, int stateCode)
    {
        InstanceId = instanceId;
        ReservationId = reservationId;
        ImageId = imageId;
        InstanceType = instanceType;
        ContainerId = containerId;
        PrivateIpAddress = privateIpAddress;
        StateName = stateName;
        StateCode = stateCode;
    }

    public string InstanceId { get; }

    public string ReservationId { get; }

    public string ImageId { get; }

    public string InstanceType { get; }

    public string ContainerId { get; }

    public string PrivateIpAddress { get; }

    public string StateName { get; }

    public int StateCode { get; }

    public Ec2Instance WithState(string stateName, int stateCode)
    {
        return new Ec2Instance(InstanceId, ReservationId, ImageId, InstanceType, ContainerId, PrivateIpAddress, stateName, stateCode);
    }
}
