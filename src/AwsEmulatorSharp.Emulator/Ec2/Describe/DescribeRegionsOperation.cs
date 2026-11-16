using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeRegionsOperation : IQueryOperation
{
    private const string RegionsActionName = "DescribeRegions";
    private const string InvalidParameterValueCode = "InvalidParameterValue";
    private const string RegionNameParameterPrefix = "RegionName.";
    private const string ResponseRootElementName = "DescribeRegionsResponse";
    private const string RequestIdElementName = "requestId";
    private const string RegionInfoElementName = "regionInfo";
    private const string ItemElementName = "item";
    private const string RegionNameElementName = "regionName";
    private const string RegionEndpointElementName = "regionEndpoint";
    private const string OptInStatusElementName = "optInStatus";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeRegionsOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => RegionsActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedRegionNames = form.ReadValues(RegionNameParameterPrefix);
        var unsupportedRegionNames = requestedRegionNames
            .Where(regionName => regionName != _configuration.Region.Name)
            .ToList();

        if (unsupportedRegionNames.Count > 0)
        {
            var unsupportedRegionName = unsupportedRegionNames.First();
            var message = $"The region '{unsupportedRegionName}' is not served by this emulator. The emulator serves {_configuration.Region.Name}.";
            await _writer.WriteErrorAsync(context.Response, InvalidParameterValueCode, message);
            return;
        }

        await _writer.WriteAsync(context.Response, BuildDocument());
    }

    private XDocument BuildDocument()
    {
        var xmlNamespace = _writer.XmlNamespace;

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + RegionInfoElementName,
                    new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + RegionNameElementName, _configuration.Region.Name),
                        new XElement(xmlNamespace + RegionEndpointElementName, _configuration.Region.Endpoint),
                        new XElement(xmlNamespace + OptInStatusElementName, _configuration.Region.OptInStatus)))));
    }
}
