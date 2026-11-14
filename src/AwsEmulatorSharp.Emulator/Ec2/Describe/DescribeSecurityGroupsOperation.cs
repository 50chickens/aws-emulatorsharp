using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeSecurityGroupsOperation : IQueryOperation
{
    private const string SecurityGroupsActionName = "DescribeSecurityGroups";
    private const string GroupIdParameterPrefix = "GroupId.";
    private const string ResponseRootElementName = "DescribeSecurityGroupsResponse";
    private const string RequestIdElementName = "requestId";
    private const string SecurityGroupInfoElementName = "securityGroupInfo";
    private const string ItemElementName = "item";
    private const string GroupIdElementName = "groupId";
    private const string GroupNameElementName = "groupName";
    private const string GroupDescriptionElementName = "groupDescription";
    private const string VpcIdElementName = "vpcId";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeSecurityGroupsOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => SecurityGroupsActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedGroupIds = form.ReadValues(GroupIdParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedGroupIds));
    }

    private XDocument BuildDocument(List<string> requestedGroupIds)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var securityGroups = _configuration.SecurityGroups.SelectRequested(requestedGroupIds, group => group.GroupId);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + SecurityGroupInfoElementName,
                    securityGroups.Select(group => new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + GroupIdElementName, group.GroupId),
                        new XElement(xmlNamespace + GroupNameElementName, group.GroupName),
                        new XElement(xmlNamespace + GroupDescriptionElementName, group.Description),
                        new XElement(xmlNamespace + VpcIdElementName, group.VpcId))))));
    }
}
