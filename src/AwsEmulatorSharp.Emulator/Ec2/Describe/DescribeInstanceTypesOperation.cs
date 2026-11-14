using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeInstanceTypesOperation : IQueryOperation
{
    private const string InstanceTypesActionName = "DescribeInstanceTypes";
    private const string InstanceTypeParameterPrefix = "InstanceType.";
    private const string ResponseRootElementName = "DescribeInstanceTypesResponse";
    private const string RequestIdElementName = "requestId";
    private const string InstanceTypeSetElementName = "instanceTypeSet";
    private const string ItemElementName = "item";
    private const string InstanceTypeElementName = "instanceType";
    private const string CurrentGenerationElementName = "currentGeneration";
    private const string FreeTierEligibleElementName = "freeTierEligible";
    private const string VCpuInfoElementName = "vCpuInfo";
    private const string DefaultVCpusElementName = "defaultVCpus";
    private const string MemoryInfoElementName = "memoryInfo";
    private const string SizeInMibElementName = "sizeInMiB";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeInstanceTypesOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => InstanceTypesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedInstanceTypes = form.ReadValues(InstanceTypeParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedInstanceTypes));
    }

    private XDocument BuildDocument(List<string> requestedInstanceTypes)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var instanceTypes = _configuration.InstanceTypes.SelectRequested(requestedInstanceTypes, instanceType => instanceType.Name);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + InstanceTypeSetElementName,
                    instanceTypes.Select(instanceType => new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + InstanceTypeElementName, instanceType.Name),
                        new XElement(xmlNamespace + CurrentGenerationElementName, instanceType.CurrentGeneration),
                        new XElement(xmlNamespace + FreeTierEligibleElementName, instanceType.FreeTierEligible),
                        new XElement(xmlNamespace + VCpuInfoElementName,
                            new XElement(xmlNamespace + DefaultVCpusElementName, instanceType.VCpuCount)),
                        new XElement(xmlNamespace + MemoryInfoElementName,
                            new XElement(xmlNamespace + SizeInMibElementName, instanceType.MemoryMib)))))));
    }
}
