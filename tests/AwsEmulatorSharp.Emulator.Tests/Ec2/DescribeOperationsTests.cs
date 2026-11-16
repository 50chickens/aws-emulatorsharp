using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2;

[TimedTestFixture(1)]
public class DescribeOperationsTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string DescribeRegionsRequest = "Action=DescribeRegions";
    private const string DescribeAvailabilityZonesRequest = "Action=DescribeAvailabilityZones";
    private const string DescribeInstanceTypesRequest = "Action=DescribeInstanceTypes";
    private const string DescribeKeyPairsRequest = "Action=DescribeKeyPairs";
    private const string DescribeSubnetsRequest = "Action=DescribeSubnets";
    private const string DescribeSecurityGroupsRequest = "Action=DescribeSecurityGroups";
    private const string RegionName = "us-east-1";
    private const string RegionEndpoint = "ec2.us-east-1.amazonaws.com";
    private const string RegionOptInStatus = "opt-in-not-required";
    private const string RegionNameFilter = "Action=DescribeRegions&RegionName.1=us-east-1";
    private const string UnknownRegionRequest = "Action=DescribeRegions&RegionName.1=not-a-region";
    private const string FirstZoneName = "us-east-1a";
    private const string SecondZoneName = "us-east-1b";
    private const string FirstZoneId = "use1-az1";
    private const string SecondZoneId = "use1-az2";
    private const string ZoneState = "available";
    private const string InstanceTypeName = "t3.micro";
    private const string SecondInstanceTypeName = "t3.medium";
    private const string TrueXmlValue = "true";
    private const string KeyName = "emulator-key";
    private const string KeyFingerprint = "1f:2e:3d:4c:5b:6a:79:88:97:a6:b5:c4:d3:e2:f1:00:0f:1e:2d:3c";
    private const string KeyType = "rsa";
    private const string SubnetId = "subnet-0a1b2c3d4e5f60718";
    private const string SubnetCidrBlock = "10.0.0.0/24";
    private const string SubnetAvailabilityZone = "us-east-1a";
    private const string SubnetIdFilterRequest = "Action=DescribeSubnets&SubnetId.1=subnet-0a1b2c3d4e5f60718";
    private const string UnknownSubnetIdRequest = "Action=DescribeSubnets&SubnetId.1=subnet-00000000000000000";
    private const string GroupId = "sg-0123456789abcdef0";
    private const string GroupName = "default";
    private const string GroupDescription = "default VPC security group";
    private const string VpcId = "vpc-0123456789abcdef0";
    private const string SubnetState = "available";
    private const int AvailabilityZoneCount = 2;
    private const int InstanceTypeCount = 2;
    private const int KeyPairCount = 1;
    private const int SubnetCount = 2;
    private const int SecurityGroupCount = 2;
    private const int RequestedItemCount = 1;
    private const int AbsentItemCount = 0;
    private const int VCpuCount = 2;
    private const int MemoryMib = 1024;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;

    [SetUp]
    public void SetUp()
    {
        var configurationReader = new Ec2ConfigurationReader();
        var configuration = configurationReader.Read();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var operations = new List<IQueryOperation>
        {
            new DescribeRegionsOperation(configuration, writer),
            new DescribeAvailabilityZonesOperation(configuration, writer),
            new DescribeInstanceTypesOperation(configuration, writer),
            new DescribeKeyPairsOperation(configuration, writer),
            new DescribeSubnetsOperation(configuration, writer),
            new DescribeSecurityGroupsOperation(configuration, writer)
        };

        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [TestCase(DescribeRegionsRequest)]
    [TestCase(RegionNameFilter)]
    public async Task Test_That_We_Can_Describe_Regions(string formBody)
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(formBody));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var regionName = document.Descendants().Single(element => element.Name.LocalName == "regionName").Value;
        var regionEndpoint = document.Descendants().Single(element => element.Name.LocalName == "regionEndpoint").Value;
        var optInStatus = document.Descendants().Single(element => element.Name.LocalName == "optInStatus").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(rootName, Is.EqualTo("DescribeRegionsResponse"));
        Assert.That(regionName, Is.EqualTo(RegionName));
        Assert.That(regionEndpoint, Is.EqualTo(RegionEndpoint));
        Assert.That(optInStatus, Is.EqualTo(RegionOptInStatus));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Availability_Zones()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeAvailabilityZonesRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var zoneNames = document.Descendants()
            .Where(element => element.Name.LocalName == "zoneName")
            .Select(element => element.Value)
            .ToList();
        var zoneStates = document.Descendants()
            .Where(element => element.Name.LocalName == "zoneState")
            .Select(element => element.Value)
            .ToList();
        var regionNames = document.Descendants()
            .Where(element => element.Name.LocalName == "regionName")
            .Select(element => element.Value)
            .ToList();
        var zoneIds = document.Descendants()
            .Where(element => element.Name.LocalName == "zoneId")
            .Select(element => element.Value)
            .ToList();

        Assert.That(rootName, Is.EqualTo("DescribeAvailabilityZonesResponse"));
        Assert.That(zoneNames, Has.Count.EqualTo(AvailabilityZoneCount));
        Assert.That(zoneNames.First(), Is.EqualTo(FirstZoneName));
        Assert.That(zoneNames.Last(), Is.EqualTo(SecondZoneName));
        Assert.That(zoneStates.First(), Is.EqualTo(ZoneState));
        Assert.That(regionNames.First(), Is.EqualTo(RegionName));
        Assert.That(zoneIds, Is.EqualTo(new[] { FirstZoneId, SecondZoneId }));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Instance_Types()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstanceTypesRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var instanceTypeNames = document.Descendants()
            .Where(element => element.Name.LocalName == "instanceType")
            .Select(element => element.Value)
            .ToList();
        var defaultVCpus = document.Descendants().First(element => element.Name.LocalName == "defaultVCpus").Value;
        var sizeInMib = document.Descendants().First(element => element.Name.LocalName == "sizeInMiB").Value;
        var burstablePerformanceSupported = document.Descendants()
            .Where(element => element.Name.LocalName == "burstablePerformanceSupported")
            .Select(element => element.Value)
            .ToList();

        Assert.That(rootName, Is.EqualTo("DescribeInstanceTypesResponse"));
        Assert.That(instanceTypeNames, Has.Count.EqualTo(InstanceTypeCount));
        Assert.That(instanceTypeNames.First(), Is.EqualTo(InstanceTypeName));
        Assert.That(instanceTypeNames.Last(), Is.EqualTo(SecondInstanceTypeName));
        Assert.That(defaultVCpus, Is.EqualTo(VCpuCount.ToString()));
        Assert.That(sizeInMib, Is.EqualTo(MemoryMib.ToString()));
        Assert.That(burstablePerformanceSupported, Is.EqualTo(new[] { TrueXmlValue, TrueXmlValue }));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Key_Pairs()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeKeyPairsRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var keyNames = document.Descendants()
            .Where(element => element.Name.LocalName == "keyName")
            .Select(element => element.Value)
            .ToList();
        var keyFingerprint = document.Descendants().Single(element => element.Name.LocalName == "keyFingerprint").Value;
        var keyType = document.Descendants().Single(element => element.Name.LocalName == "keyType").Value;

        Assert.That(rootName, Is.EqualTo("DescribeKeyPairsResponse"));
        Assert.That(keyNames, Has.Count.EqualTo(KeyPairCount));
        Assert.That(keyNames.First(), Is.EqualTo(KeyName));
        Assert.That(keyFingerprint, Is.EqualTo(KeyFingerprint));
        Assert.That(keyType, Is.EqualTo(KeyType));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Subnets()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeSubnetsRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var subnetIds = document.Descendants()
            .Where(element => element.Name.LocalName == "subnetId")
            .Select(element => element.Value)
            .ToList();
        var cidrBlock = document.Descendants().First(element => element.Name.LocalName == "cidrBlock").Value;
        var availabilityZone = document.Descendants().First(element => element.Name.LocalName == "availabilityZone").Value;
        var vpcId = document.Descendants().First(element => element.Name.LocalName == "vpcId").Value;
        var state = document.Descendants().First(element => element.Name.LocalName == "state").Value;
        var defaultForAz = document.Descendants().First(element => element.Name.LocalName == "defaultForAz").Value;

        Assert.That(rootName, Is.EqualTo("DescribeSubnetsResponse"));
        Assert.That(subnetIds, Has.Count.EqualTo(SubnetCount));
        Assert.That(subnetIds.First(), Is.EqualTo(SubnetId));
        Assert.That(cidrBlock, Is.EqualTo(SubnetCidrBlock));
        Assert.That(availabilityZone, Is.EqualTo(SubnetAvailabilityZone));
        Assert.That(vpcId, Is.EqualTo(VpcId));
        Assert.That(state, Is.EqualTo(SubnetState));
        Assert.That(defaultForAz, Is.EqualTo("true"));
    }

    [TestCase(SubnetIdFilterRequest, RequestedItemCount)]
    [TestCase(UnknownSubnetIdRequest, AbsentItemCount)]
    public async Task Test_That_We_Can_Select_A_Subnet_By_Id(string formBody, int expectedCount)
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(formBody));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var subnetIds = document.Descendants()
            .Where(element => element.Name.LocalName == "subnetId")
            .Select(element => element.Value)
            .ToList();

        Assert.That(subnetIds, Has.Count.EqualTo(expectedCount));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Security_Groups()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeSecurityGroupsRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var groupIds = document.Descendants()
            .Where(element => element.Name.LocalName == "groupId")
            .Select(element => element.Value)
            .ToList();
        var groupName = document.Descendants().First(element => element.Name.LocalName == "groupName").Value;
        var groupDescription = document.Descendants().First(element => element.Name.LocalName == "groupDescription").Value;
        var vpcId = document.Descendants().First(element => element.Name.LocalName == "vpcId").Value;

        Assert.That(rootName, Is.EqualTo("DescribeSecurityGroupsResponse"));
        Assert.That(groupIds, Has.Count.EqualTo(SecurityGroupCount));
        Assert.That(groupIds.First(), Is.EqualTo(GroupId));
        Assert.That(groupName, Is.EqualTo(GroupName));
        Assert.That(groupDescription, Is.EqualTo(GroupDescription));
        Assert.That(vpcId, Is.EqualTo(VpcId));
    }

    [Test]
    public async Task Test_That_We_Can_Answer_An_Unknown_Region_In_The_Error_Element_Shape()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(UnknownRegionRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == "Code").Value;
        var errorMessage = document.Descendants().Single(element => element.Name.LocalName == "Message").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo("InvalidParameterValue"));
        Assert.That(errorMessage, Does.Contain("not-a-region"));
    }
}
