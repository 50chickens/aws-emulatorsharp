using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeAvailabilityZonesOperation : IQueryOperation
{
    private const string AvailabilityZonesActionName = "DescribeAvailabilityZones";
    private const string ZoneNameParameterPrefix = "ZoneName.";
    private const string ResponseRootElementName = "DescribeAvailabilityZonesResponse";
    private const string RequestIdElementName = "requestId";
    private const string AvailabilityZoneInfoElementName = "availabilityZoneInfo";
    private const string ItemElementName = "item";
    private const string ZoneNameElementName = "zoneName";
    private const string ZoneStateElementName = "zoneState";
    private const string RegionNameElementName = "regionName";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeAvailabilityZonesOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => AvailabilityZonesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedZoneNames = form.ReadValues(ZoneNameParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedZoneNames));
    }

    private XDocument BuildDocument(List<string> requestedZoneNames)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var zones = _configuration.AvailabilityZones.SelectRequested(requestedZoneNames, zone => zone.Name);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + AvailabilityZoneInfoElementName,
                    zones.Select(zone => new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + ZoneNameElementName, zone.Name),
                        new XElement(xmlNamespace + ZoneStateElementName, zone.State),
                        new XElement(xmlNamespace + RegionNameElementName, zone.RegionName))))));
    }
}
