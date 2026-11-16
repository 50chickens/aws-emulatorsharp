using System.Globalization;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeSecurityGroupsOperation : IQueryOperation
{
    private const string SecurityGroupsActionName = "DescribeSecurityGroups";
    private const string GroupIdParameterPrefix = "GroupId.";
    private const string GroupNameParameterPrefix = "GroupName.";
    private const string InvalidGroupNotFoundCode = "InvalidGroup.NotFound";
    private const string GroupNotFoundMessageFormat = "The security group '{0}' does not exist";
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
        var requestedGroupNames = form.ReadValues(GroupNameParameterPrefix);
        var unknownGroupNames = requestedGroupNames
            .Where(groupName => !_configuration.SecurityGroups.Any(group => group.GroupName == groupName))
            .ToList();

        if (unknownGroupNames.Count > 0)
        {
            var unknownGroupName = unknownGroupNames.First();
            var message = string.Format(CultureInfo.InvariantCulture, GroupNotFoundMessageFormat, unknownGroupName);
            await _writer.WriteErrorAsync(context.Response, InvalidGroupNotFoundCode, message);
            return;
        }

        await _writer.WriteAsync(context.Response, BuildDocument(requestedGroupIds, requestedGroupNames));
    }

    private XDocument BuildDocument(List<string> requestedGroupIds, List<string> requestedGroupNames)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var securityGroupsById = _configuration.SecurityGroups.SelectRequested(requestedGroupIds, group => group.GroupId);
        var securityGroups = securityGroupsById.SelectRequested(requestedGroupNames, group => group.GroupName);

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
