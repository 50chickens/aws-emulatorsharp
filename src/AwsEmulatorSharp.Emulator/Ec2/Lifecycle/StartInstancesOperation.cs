using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class StartInstancesOperation : IQueryOperation
{
    private const string StartInstancesActionName = "StartInstances";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "StartInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public StartInstancesOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => StartInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);
        var startedInstances = _store.Find(requestedInstanceIds).Select(StartInstance).ToList();

        await _writer.WriteAsync(context.Response, BuildDocument(startedInstances));
    }

    private Ec2Instance StartInstance(Ec2Instance instance)
    {
        _containers.StartExisting(instance.ContainerId);
        var startedInstance = _store.Start(instance);

        return startedInstance;
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
