using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.UserData;

public class GetConsoleOutputOperation : IQueryOperation
{
    private const string GetConsoleOutputActionName = "GetConsoleOutput";
    private const string InstanceIdParameterName = "InstanceId";
    private const string ResponseRootElementName = "GetConsoleOutputResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstanceIdElementName = "instanceId";
    private const string OutputElementName = "output";

    private readonly InstanceStore _store;
    private readonly QueryResponseWriter _writer;

    public GetConsoleOutputOperation(InstanceStore store, QueryResponseWriter writer)
    {
        _store = store;
        _writer = writer;
    }

    public string ActionName => GetConsoleOutputActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var instanceId = form[InstanceIdParameterName].ToString();
        var consoleOutput = _store.ReadConsoleOutput(instanceId);

        await _writer.WriteAsync(context.Response, BuildDocument(instanceId, consoleOutput));
    }

    private XDocument BuildDocument(string instanceId, string consoleOutput)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var encodedOutput = Convert.ToBase64String(Encoding.UTF8.GetBytes(consoleOutput));

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + InstanceIdElementName, instanceId),
                new XElement(xmlNamespace + OutputElementName, encodedOutput)));
    }
}
