using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class DescribeInstancesOperation : IQueryOperation
{
    private const string DescribeInstancesActionName = "DescribeInstances";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "DescribeInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string ReservationSetElementName = "reservationSet";
    private const string ItemElementName = "item";
    private const string ReservationIdElementName = "reservationId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public DescribeInstancesOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => DescribeInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedInstanceIds));
    }

    private XDocument BuildDocument(List<string> requestedInstanceIds)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var instances = _store.Find(requestedInstanceIds);

        var reservationItems = instances
            .GroupBy(instance => instance.ReservationId)
            .Select(reservationGroup => BuildReservationItem(xmlNamespace, reservationGroup));

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + ReservationSetElementName, reservationItems)));
    }

    private XElement BuildReservationItem(XNamespace xmlNamespace, IGrouping<string, Ec2Instance> reservationGroup)
    {
        var instanceItems = reservationGroup
            .Select(instance => instance.ToInstanceItem(xmlNamespace, _containers.Address(instance.ContainerId)));

        return new XElement(xmlNamespace + ItemElementName,
            new XElement(xmlNamespace + ReservationIdElementName, reservationGroup.Key),
            new XElement(xmlNamespace + InstancesSetElementName, instanceItems));
    }
}
