using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class RebootInstancesOperation : IQueryOperation
{
    private const string RebootInstancesActionName = "RebootInstances";
    private const string InstanceIdParameterPrefix = "InstanceId.";
    private const string ResponseRootElementName = "RebootInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public RebootInstancesOperation(InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => RebootInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceIds = form.ReadValues(InstanceIdParameterPrefix);
        var rebootedInstances = _store.Find(requestedInstanceIds).Select(RebootInstance).ToList();

        await _writer.WriteAsync(context.Response, BuildDocument(rebootedInstances));
    }

    private Ec2Instance RebootInstance(Ec2Instance instance)
    {
        _containers.Restart(instance.ContainerId);
        var rebootedInstance = _store.Start(instance);

        return rebootedInstance;
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
