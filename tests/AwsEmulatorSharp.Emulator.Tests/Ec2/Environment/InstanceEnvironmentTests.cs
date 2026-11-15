using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Tests.Identity;
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
    private const string RunInstancesWithProfileRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&IamInstanceProfile.Name={2}";
    private const string InstanceIdElementName = "instanceId";
    private const string PrivateIpAddressElementName = "privateIpAddress";
    private const string ArnElementName = "Arn";
    private const string InstanceIdVariableName = "AWS_EC2_INSTANCE_ID";
    private const string PrivateIpAddressVariableName = "AWS_EC2_PRIVATE_IP";
    private const string RegionVariableName = "AWS_REGION";
    private const string AccessKeyIdVariableName = "AWS_ACCESS_KEY_ID";
    private const string SecretAccessKeyVariableName = "AWS_SECRET_ACCESS_KEY";
    private const string EndpointUrlVariableName = "AWS_ENDPOINT_URL";
    private const string ContainerHostName = "host.docker.internal";
    private const string EndpointFormat = "http://{0}:{1}";
    private const string FirstContainerId = "container-1";
    private const int SuccessfulExitCode = 0;

    private Ec2Configuration _configuration;
    private IdentityConfiguration _identityConfiguration;
    private HostConfiguration _hostConfiguration;
    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private InMemoryContainerDaemon _daemon;
    private IdentityRequestFactory _requestFactory;
    private string _imageId;
    private string _instanceType;
    private string _instanceProfileName;

    [SetUp]
    public void SetUp()
    {
        _configuration = new Ec2ConfigurationReader().Read();
        _identityConfiguration = new IdentityConfigurationReader().Read();
        _hostConfiguration = new HostConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, SuccessfulExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon, new ContainersConfigurationReader().Read());
        var environment = new Ec2InstanceEnvironment(_identityConfiguration, _hostConfiguration);
        var identityStore = new IdentityStore(_identityConfiguration, TimeProvider.System);
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(_configuration, store, containers, environment, writer),
            new GetCallerIdentityOperation(identityStore, new IdentityResponseWriter())
        };

        _imageId = _configuration.AmiToImage.Keys.First();
        _instanceType = _configuration.InstanceTypes.First().Name;
        _instanceProfileName = _identityConfiguration.InstanceProfiles.Keys.First();
        _requestFactory = new IdentityRequestFactory();
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

    [Test]
    public async Task Test_That_The_Launched_Container_Is_Given_The_Instance_Profile_Credential_In_Its_Environment()
    {
        await LaunchInstanceWithProfileAsync();

        var environment = _daemon.LastEnvironment;
        var profile = _identityConfiguration.InstanceProfiles[_instanceProfileName];

        Assert.That(environment[AccessKeyIdVariableName], Is.EqualTo(profile.AccessKeyId));
        Assert.That(environment[SecretAccessKeyVariableName], Is.EqualTo(profile.SecretAccessKey));
    }

    [Test]
    public async Task Test_That_The_Launched_Container_Is_Given_The_Emulator_Endpoint_In_Its_Environment()
    {
        await LaunchInstanceWithProfileAsync();

        var environment = _daemon.LastEnvironment;
        var endpoint = string.Format(CultureInfo.InvariantCulture, EndpointFormat, ContainerHostName, _hostConfiguration.ListenerPort);

        Assert.That(environment[EndpointUrlVariableName], Is.EqualTo(endpoint));
    }

    [Test]
    public async Task Test_That_A_GetCallerIdentity_From_The_Launched_Container_Returns_The_Arn_The_Profile_Names()
    {
        await LaunchInstanceWithProfileAsync();

        var accessKeyId = _daemon.LastEnvironment[AccessKeyIdVariableName];
        var context = _requestFactory.CreateGetCallerIdentityRequest(accessKeyId, string.Empty);

        await _surface.HandleAsync(context);

        var document = context.ReadResponseDocument();
        var arn = document.Descendants().Single(element => element.Name.LocalName == ArnElementName).Value;

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(arn, Is.EqualTo(_identityConfiguration.InstanceProfiles[_instanceProfileName].Arn));
    }

    private async Task LaunchInstanceAsync()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesRequestFormat,
            _imageId,
            _instanceType);

        await SendRunInstancesAsync(request);
    }

    private async Task LaunchInstanceWithProfileAsync()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesWithProfileRequestFormat,
            _imageId,
            _instanceType,
            _instanceProfileName);

        await SendRunInstancesAsync(request);
    }

    private async Task SendRunInstancesAsync(string request)
    {
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
