using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class StopInstancesOperation : IQueryOperation
{
    private const string StopInstancesActionName = "StopInstances";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "StopInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public StopInstancesOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => StopInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);
        var stoppedInstances = _store.Find(requestedInstanceIds).Select(StopInstance).ToList();

        await _writer.WriteAsync(context.Response, BuildDocument(stoppedInstances));
    }

    private Ec2Instance StopInstance(Ec2Instance instance)
    {
        _containers.Stop(instance.ContainerId);
        var stoppedInstance = _store.Stop(instance);

        return stoppedInstance;
    }

    private XDocument BuildDocument(IReadOnlyList<Ec2Instance> instances)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var instanceItems = instances.Select(instance => instance.ToInstanceStateItem(xmlNamespace));

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + InstancesSetElementName, instanceItems)));
    }
}
