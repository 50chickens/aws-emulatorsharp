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
public class UserDataTerminatePolicyTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&UserData={2}";
    private const string DescribeInstancesRequest = "Action=DescribeInstances";
    private const string DescribeInstancesResponseRootName = "DescribeInstancesResponse";
    private const string InstanceIdElementName = "instanceId";
    private const string StateCodeElementName = "code";
    private const string StateNameElementName = "name";
    private const string StateCodeTerminated = "48";
    private const string StateNameTerminated = "terminated";
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
        configuration.UserDataFailurePolicy = Ec2UserDataFailurePolicy.Terminate;
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, FailingStandardError, FailingExitCode);
        var daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(daemon, new ContainersConfigurationReader().Read());
        var environment = new Ec2InstanceEnvironment(new IdentityConfigurationReader().Read(), new HostConfigurationReader().Read());
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, environment, writer),
            new DescribeInstancesOperation(store, writer)
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
    public async Task Test_That_We_Move_The_Instance_To_Terminated_When_Its_UserData_Script_Fails_Under_The_Terminate_Policy()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;
        var launchedStateCode = launchDocument.Descendants().Single(element => element.Name.LocalName == StateCodeElementName).Value;
        var launchedStateName = launchDocument.Descendants().Single(element => element.Name.LocalName == StateNameElementName).Value;

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstancesRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var describeDocument = XDocument.Load(_responseBody);
        var describeRootName = describeDocument.Elements().Single().Name.LocalName;
        var describedInstanceId = describeDocument.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;
        var describedStateCode = describeDocument.Descendants().Single(element => element.Name.LocalName == StateCodeElementName).Value;
        var describedStateName = describeDocument.Descendants().Single(element => element.Name.LocalName == StateNameElementName).Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(launchedStateCode, Is.EqualTo(StateCodeTerminated));
        Assert.That(launchedStateName, Is.EqualTo(StateNameTerminated));
        Assert.That(describeRootName, Is.EqualTo(DescribeInstancesResponseRootName));
        Assert.That(describedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(describedStateCode, Is.EqualTo(StateCodeTerminated));
        Assert.That(describedStateName, Is.EqualTo(StateNameTerminated));
    }
}
