using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.InstanceEnvironment;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Ec2.UserData;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.UserData;

[TimedTestFixture(1)]
public class UserDataRebootStageTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&UserData={2}";
    private const string DescribeInstanceStatusRequest = "Action=DescribeInstanceStatus";
    private const string GetConsoleOutputRequestFormat = "Action=GetConsoleOutput&InstanceId={0}";
    private const string DescribeInstanceStatusResponseRootName = "DescribeInstanceStatusResponse";
    private const string GetConsoleOutputResponseRootName = "GetConsoleOutputResponse";
    private const string InstanceIdElementName = "instanceId";
    private const string InstanceStateElementName = "instanceState";
    private const string InstanceStatusElementName = "instanceStatus";
    private const string StateCodeElementName = "code";
    private const string StateNameElementName = "name";
    private const string StatusElementName = "status";
    private const string BootCountElementName = "bootCount";
    private const string OutputElementName = "output";
    private const string StateCodeRunning = "16";
    private const string StateNameRunning = "running";
    private const string SecondBootCount = "2";
    private const string StatusOk = "ok";
    private const string FirstContainerId = "container-1";
    private const string FirstStageLine = "Write-Output 'first-stage-ran'";
    private const string RebootRequestLine = "Restart-Computer -Force";
    private const string SecondStageLine = "Write-Output 'second-stage-ran'";
    private const string FirstStageOutput = "first-stage-ran";
    private const string SecondStageOutput = "second-stage-ran";
    private const string EmptyStandardError = "";
    private const int StoppedSessionExitCode = 1;
    private const int SuccessfulExitCode = 0;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private InMemoryContainerDaemon _daemon;
    private string _imageId;
    private string _instanceType;
    private string _encodedUserData;
    private string _firstStageScript;
    private string _secondStageScript;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        _firstStageScript = string.Join(System.Environment.NewLine, FirstStageLine, RebootRequestLine);
        _secondStageScript = SecondStageLine;
        var script = string.Join(System.Environment.NewLine, _firstStageScript, _secondStageScript);
        var commandResults = new List<ContainerCommandResult>
        {
            new ContainerCommandResult(FirstStageOutput, EmptyStandardError, StoppedSessionExitCode),
            new ContainerCommandResult(SecondStageOutput, EmptyStandardError, SuccessfulExitCode)
        };
        _daemon = new InMemoryContainerDaemon(commandResults);
        var containers = new ContainersSurface(_daemon, new ContainersConfigurationReader().Read());
        var environment = new Ec2InstanceEnvironment(new IdentityConfigurationReader().Read(), new HostConfigurationReader().Read());
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, environment, writer),
            new DescribeInstanceStatusOperation(store, containers, writer),
            new GetConsoleOutputOperation(store, writer)
        };

        _imageId = configuration.AmiToImage.Keys.First();
        _instanceType = configuration.InstanceTypes.First().Name;
        _encodedUserData = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(script)));
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Record_A_User_Data_Stage_That_Requests_A_Reboot_As_A_Boundary_Rather_Than_A_Failed_Run()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedStateCode = launchDocument.Descendants().Single(element => element.Name.LocalName == StateCodeElementName).Value;
        var launchedStateName = launchDocument.Descendants().Single(element => element.Name.LocalName == StateNameElementName).Value;
        var launchedBootCount = launchDocument.Descendants().Single(element => element.Name.LocalName == BootCountElementName).Value;

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
        var instanceStatusName = statusDocument
            .Descendants()
            .Single(element => element.Name.LocalName == InstanceStatusElementName)
            .Elements()
            .Single(element => element.Name.LocalName == StatusElementName)
            .Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(launchedStateCode, Is.EqualTo(StateCodeRunning));
        Assert.That(launchedStateName, Is.EqualTo(StateNameRunning));
        Assert.That(launchedBootCount, Is.EqualTo(SecondBootCount));
        Assert.That(statusRootName, Is.EqualTo(DescribeInstanceStatusResponseRootName));
        Assert.That(instanceStatusName, Is.EqualTo(StatusOk));
    }

    [Test]
    public async Task Test_That_We_Run_The_User_Data_Stage_After_The_Boundary_Once_The_Instance_Has_Restarted()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        var expectedJournal = new List<ContainerDaemonOperation>
        {
            ContainerDaemonOperation.RunCommand,
            ContainerDaemonOperation.RestartContainer,
            ContainerDaemonOperation.RunCommand
        };
        var expectedCommands = new List<string>
        {
            _firstStageScript,
            _secondStageScript
        };

        Assert.That(_daemon.Journal, Is.EqualTo(expectedJournal));
        Assert.That(_daemon.Commands, Is.EqualTo(expectedCommands));
        Assert.That(_daemon.LastRestartedContainerId, Is.EqualTo(FirstContainerId));
        Assert.That(_daemon.StartedContainerCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Test_That_We_Record_The_Output_Of_Every_User_Data_Stage()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        var consoleOutputRequest = string.Format(CultureInfo.InvariantCulture, GetConsoleOutputRequestFormat, launchedInstanceId);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(consoleOutputRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var encodedOutput = document.Descendants().Single(element => element.Name.LocalName == OutputElementName).Value;
        var consoleOutput = Encoding.UTF8.GetString(Convert.FromBase64String(encodedOutput));

        Assert.That(rootName, Is.EqualTo(GetConsoleOutputResponseRootName));
        Assert.That(consoleOutput, Does.Contain(FirstStageOutput));
        Assert.That(consoleOutput, Does.Contain(SecondStageOutput));
    }
}
