namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class Ec2Instance
{
    public Ec2Instance(string instanceId, string reservationId, string imageId, string instanceType, string stateName, int stateCode)
    {
        InstanceId = instanceId;
        ReservationId = reservationId;
        ImageId = imageId;
        InstanceType = instanceType;
        StateName = stateName;
        StateCode = stateCode;
    }

    public string InstanceId { get; }

    public string ReservationId { get; }

    public string ImageId { get; }

    public string InstanceType { get; }

    public string StateName { get; }

    public int StateCode { get; }
}
