using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class TerminateInstancesOperation : IQueryOperation
{
    private const string TerminateInstancesActionName = "TerminateInstances";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "TerminateInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public TerminateInstancesOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => TerminateInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);
        var terminatedInstances = _store.Find(requestedInstanceIds).Select(TerminateInstance).ToList();

        await _writer.WriteAsync(context.Response, BuildDocument(terminatedInstances));
    }

    private Ec2Instance TerminateInstance(Ec2Instance instance)
    {
        _containers.Remove(instance.ContainerId);
        var terminatedInstance = _store.Terminate(instance);

        return terminatedInstance;
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
