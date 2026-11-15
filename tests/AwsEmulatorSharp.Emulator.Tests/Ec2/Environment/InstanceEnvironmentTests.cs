using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.Environment;

[TimedTestFixture(1)]
public class InstanceEnvironmentTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}";
    private const string InstanceIdElementName = "instanceId";
    private const string PrivateIpAddressElementName = "privateIpAddress";
    private const string InstanceIdVariableName = "AWS_EC2_INSTANCE_ID";
    private const string PrivateIpAddressVariableName = "AWS_EC2_PRIVATE_IP";
    private const string RegionVariableName = "AWS_REGION";
    private const string FirstContainerId = "container-1";
    private const int SuccessfulExitCode = 0;

    private Ec2Configuration _configuration;
    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private InMemoryContainerDaemon _daemon;
    private string _imageId;
    private string _instanceType;

    [SetUp]
    public void SetUp()
    {
        _configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, SuccessfulExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon);
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(_configuration, store, containers, writer)
        };

        _imageId = _configuration.AmiToImage.Keys.First();
        _instanceType = _configuration.InstanceTypes.First().Name;
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_The_Launched_Container_Is_Given_The_Instance_Id_In_Its_Environment()
    {
        await LaunchInstanceAsync();

        var instanceId = ReadResponseElement(InstanceIdElementName);
        var environment = _daemon.LastEnvironment;

        Assert.That(_daemon.LastEnvironmentContainerId, Is.EqualTo(FirstContainerId));
        Assert.That(environment[InstanceIdVariableName], Is.EqualTo(instanceId));
    }

    [Test]
    public async Task Test_That_The_Launched_Container_Is_Given_The_Private_Ip_Address_In_Its_Environment()
    {
        await LaunchInstanceAsync();

        var privateIpAddress = ReadResponseElement(PrivateIpAddressElementName);
        var environment = _daemon.LastEnvironment;

        Assert.That(environment[PrivateIpAddressVariableName], Is.EqualTo(privateIpAddress));
    }

    [Test]
    public async Task Test_That_The_Launched_Container_Is_Given_The_Region_In_Its_Environment()
    {
        await LaunchInstanceAsync();

        var environment = _daemon.LastEnvironment;

        Assert.That(environment[RegionVariableName], Is.EqualTo(_configuration.Region.Name));
    }

    private async Task LaunchInstanceAsync()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesRequestFormat,
            _imageId,
            _instanceType);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    private string ReadResponseElement(string elementName)
    {
        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var value = document.Descendants().Single(element => element.Name.LocalName == elementName).Value;
        return value;
    }
}
