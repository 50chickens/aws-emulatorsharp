using System.Xml.Linq;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

internal static class Ec2InstanceResponseExtensions
{
    private const string ItemElementName = "item";
    private const string InstanceIdElementName = "instanceId";
    private const string ImageIdElementName = "imageId";
    private const string InstanceStateElementName = "instanceState";
    private const string StateCodeElementName = "code";
    private const string StateNameElementName = "name";
    private const string InstanceTypeElementName = "instanceType";

    internal static XElement ToInstanceItem(this Ec2Instance instance, XNamespace xmlNamespace)
    {
        return new XElement(xmlNamespace + ItemElementName,
            new XElement(xmlNamespace + InstanceIdElementName, instance.InstanceId),
            new XElement(xmlNamespace + ImageIdElementName, instance.ImageId),
            new XElement(xmlNamespace + InstanceStateElementName,
                new XElement(xmlNamespace + StateCodeElementName, instance.StateCode),
                new XElement(xmlNamespace + StateNameElementName, instance.StateName)),
            new XElement(xmlNamespace + InstanceTypeElementName, instance.InstanceType));
    }
}
