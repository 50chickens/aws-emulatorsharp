using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public class DescribeRegionsOperation
{
    private const string InvalidParameterValueCode = "InvalidParameterValue";
    private const string RegionNameParameterPrefix = "RegionName.";
    private const string ResponseRootElementName = "DescribeRegionsResponse";
    private const string RequestIdElementName = "requestId";
    private const string RegionInfoElementName = "regionInfo";
    private const string ItemElementName = "item";
    private const string RegionNameElementName = "regionName";
    private const string RegionEndpointElementName = "regionEndpoint";

    private readonly IWireConfiguration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeRegionsOperation(IWireConfiguration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedRegionNames = form
            .Where(parameter => parameter.Key.StartsWith(RegionNameParameterPrefix))
            .Select(parameter => parameter.Value.ToString())
            .ToList();

        var unsupportedRegionNames = requestedRegionNames
            .Where(regionName => regionName != _configuration.RegionName)
            .ToList();

        if (unsupportedRegionNames.Count > 0)
        {
            var unsupportedRegionName = unsupportedRegionNames.First();
            var message = $"The region '{unsupportedRegionName}' is not served by this emulator. The emulator serves {_configuration.RegionName}.";
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
                        new XElement(xmlNamespace + RegionNameElementName, _configuration.RegionName),
                        new XElement(xmlNamespace + RegionEndpointElementName, _configuration.RegionEndpoint)))));
    }
}
