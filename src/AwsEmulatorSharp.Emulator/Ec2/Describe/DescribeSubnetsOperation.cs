using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeSubnetsOperation : IQueryOperation
{
    private const string SubnetsActionName = "DescribeSubnets";
    private const string SubnetIdParameterPrefix = "SubnetId.";
    private const string AvailableSubnetState = "available";
    private const string ResponseRootElementName = "DescribeSubnetsResponse";
    private const string RequestIdElementName = "requestId";
    private const string SubnetSetElementName = "subnetSet";
    private const string ItemElementName = "item";
    private const string SubnetIdElementName = "subnetId";
    private const string StateElementName = "state";
    private const string VpcIdElementName = "vpcId";
    private const string CidrBlockElementName = "cidrBlock";
    private const string AvailabilityZoneElementName = "availabilityZone";
    private const string DefaultForAzElementName = "defaultForAz";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeSubnetsOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => SubnetsActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedSubnetIds = form.ReadValues(SubnetIdParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedSubnetIds));
    }

    private XDocument BuildDocument(List<string> requestedSubnetIds)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var subnets = _configuration.Subnets.SelectRequested(requestedSubnetIds, subnet => subnet.SubnetId);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + SubnetSetElementName,
                    subnets.Select(subnet => new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + SubnetIdElementName, subnet.SubnetId),
                        new XElement(xmlNamespace + StateElementName, AvailableSubnetState),
                        new XElement(xmlNamespace + VpcIdElementName, subnet.VpcId),
                        new XElement(xmlNamespace + CidrBlockElementName, subnet.CidrBlock),
                        new XElement(xmlNamespace + AvailabilityZoneElementName, subnet.AvailabilityZone),
                        new XElement(xmlNamespace + DefaultForAzElementName, subnet.DefaultForAz))))));
    }
}
