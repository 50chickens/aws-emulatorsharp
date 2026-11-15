using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class DescribeInstanceStatusOperation : IQueryOperation
{
    private const string DescribeInstanceStatusActionName = "DescribeInstanceStatus";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "DescribeInstanceStatusResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstanceStatusSetElementName = "instanceStatusSet";
    private const string OkStatusName = "ok";
    private const string NotOkStatusName = "not ok";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public DescribeInstanceStatusOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => DescribeInstanceStatusActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedInstanceIds));
    }

    private XDocument BuildDocument(List<string> requestedInstanceIds)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var instances = _store.Find(requestedInstanceIds);
        var instanceItems = instances.Select(instance => instance.ToInstanceStatusItem(xmlNamespace, StatusName(instance)));

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + InstanceStatusSetElementName, instanceItems)));
    }

    private string StatusName(Ec2Instance instance)
    {
        var isRunning = _containers.IsRunning(instance.ContainerId);
        var statusName = isRunning ? OkStatusName : NotOkStatusName;

        return statusName;
    }
}
