using System.Globalization;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class RunInstancesOperation : IQueryOperation
{
    private const string RunInstancesActionName = "RunInstances";
    private const string ImageIdParameterName = "ImageId";
    private const string InstanceTypeParameterName = "InstanceType";
    private const string InvalidImageIdNotFoundCode = "InvalidAMIID.NotFound";
    private const string ImageNotFoundMessageFormat = "The image id '[{0}]' does not exist";
    private const string ResponseRootElementName = "RunInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string ReservationIdElementName = "reservationId";
    private const string InstancesSetElementName = "instancesSet";

    private readonly Ec2Configuration _configuration;
    private readonly InstanceStore _store;
    private readonly QueryResponseWriter _writer;

    public RunInstancesOperation(Ec2Configuration configuration, InstanceStore store, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _store = store;
        _writer = writer;
    }

    public string ActionName => RunInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var imageId = form[ImageIdParameterName].ToString();
        if (!_configuration.AmiToImage.ContainsKey(imageId))
        {
            var message = string.Format(CultureInfo.InvariantCulture, ImageNotFoundMessageFormat, imageId);
            await _writer.WriteErrorAsync(context.Response, InvalidImageIdNotFoundCode, message);
            return;
        }

        var instanceType = form[InstanceTypeParameterName].ToString();
        var instance = _store.Launch(imageId, instanceType);

        await _writer.WriteAsync(context.Response, BuildDocument(instance));
    }

    private XDocument BuildDocument(Ec2Instance instance)
    {
        var xmlNamespace = _writer.XmlNamespace;

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + ReservationIdElementName, instance.ReservationId),
                new XElement(xmlNamespace + InstancesSetElementName,
                    instance.ToInstanceItem(xmlNamespace))));
    }
}
