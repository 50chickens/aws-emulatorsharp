using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.UserData;

[TimedTestFixture(1)]
public class UserDataStayRunningPolicyTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&UserData={2}";
    private const string DescribeInstanceStatusRequest = "Action=DescribeInstanceStatus";
    private const string DescribeInstanceStatusResponseRootName = "DescribeInstanceStatusResponse";
    private const string InstanceIdElementName = "instanceId";
    private const string InstanceStateElementName = "instanceState";
    private const string InstanceStatusElementName = "instanceStatus";
    private const string StatusElementName = "status";
    private const string StateCodeElementName = "code";
    private const string StateNameElementName = "name";
    private const string StateCodeRunning = "16";
    private const string StateNameRunning = "running";
    private const string StatusNotOk = "not ok";
    private const string FailingScriptLine = "throw 'user-data-failed'";
    private const string FailingStandardError = "user-data-failed";
    private const int FailingExitCode = 1;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private string _imageId;
    private string _instanceType;
    private string _encodedUserData;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, FailingStandardError, FailingExitCode);
        var daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(daemon, new ContainersConfigurationReader().Read());
        var environment = new Ec2InstanceEnvironment(new IdentityConfigurationReader().Read(), new HostConfigurationReader().Read());
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, environment, writer),
            new DescribeInstanceStatusOperation(store, containers, writer)
        };

        _imageId = configuration.AmiToImage.Keys.First();
        _instanceType = configuration.InstanceTypes.First().Name;
        _encodedUserData = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(FailingScriptLine)));
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Leave_The_Instance_Running_With_A_Status_That_Reports_The_Failure_When_Its_UserData_Script_Fails()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedStateCode = launchDocument.Descendants().Single(element => element.Name.LocalName == StateCodeElementName).Value;
        var launchedStateName = launchDocument.Descendants().Single(element => element.Name.LocalName == StateNameElementName).Value;
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstanceStatusRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var statusDocument = XDocument.Load(_responseBody);
        var statusRootName = statusDocument.Elements().Single().Name.LocalName;
        var statusInstanceId = statusDocument.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;
        var statusStateName = statusDocument
            .Descendants()
            .Single(element => element.Name.LocalName == InstanceStateElementName)
            .Elements()
            .Single(element => element.Name.LocalName == StateNameElementName)
            .Value;
        var instanceStatusName = statusDocument
            .Descendants()
            .Single(element => element.Name.LocalName == InstanceStatusElementName)
            .Elements()
            .Single(element => element.Name.LocalName == StatusElementName)
            .Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(launchedStateCode, Is.EqualTo(StateCodeRunning));
        Assert.That(launchedStateName, Is.EqualTo(StateNameRunning));
        Assert.That(statusRootName, Is.EqualTo(DescribeInstanceStatusResponseRootName));
        Assert.That(statusInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(statusStateName, Is.EqualTo(StateNameRunning));
        Assert.That(instanceStatusName, Is.EqualTo(StatusNotOk));
    }
}
