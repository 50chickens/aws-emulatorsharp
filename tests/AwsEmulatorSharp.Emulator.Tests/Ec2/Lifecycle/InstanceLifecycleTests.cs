using System.Globalization;
using System.Net;
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

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.Lifecycle;

[TimedTestFixture(1)]
public class InstanceLifecycleTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string DescribeInstancesRequest = "Action=DescribeInstances";
    private const string StopInstancesRequestFormat = "Action=StopInstances&InstanceId.1={0}";
    private const string StartInstancesRequestFormat = "Action=StartInstances&InstanceId.1={0}";
    private const string RebootInstancesRequestFormat = "Action=RebootInstances&InstanceId.1={0}";
    private const string TerminateInstancesRequestFormat = "Action=TerminateInstances&InstanceId.1={0}";
    private const string DescribeInstanceStatusRequest = "Action=DescribeInstanceStatus";
    private const string StopInstancesResponseRootName = "StopInstancesResponse";
    private const string StartInstancesResponseRootName = "StartInstancesResponse";
    private const string RebootInstancesResponseRootName = "RebootInstancesResponse";
    private const string TerminateInstancesResponseRootName = "TerminateInstancesResponse";
    private const string DescribeInstanceStatusResponseRootName = "DescribeInstanceStatusResponse";
    private const string InstanceIdPrefix = "i-";
    private const string FirstContainerId = "container-1";
    private const string StateCodeRunning = "16";
    private const string StateNameRunning = "running";
    private const string StateCodeStopped = "80";
    private const string StateNameStopped = "stopped";
    private const string StateCodeTerminated = "48";
    private const string StateNameTerminated = "terminated";
    private const string FirstBootCount = "1";
    private const string SecondBootCount = "2";
    private const string StatusOk = "ok";
    private const string StatusNotOk = "not ok";
    private const string FirstContainerAddress = "10.0.0.1";
    private const string UnknownImageId = "ami-00000000000000000";
    private const string InvalidImageIdNotFoundCode = "InvalidAMIID.NotFound";
    private const int LaunchedInstanceCount = 1;
    private const int EmptyContainerCount = 0;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private InMemoryContainerDaemon _daemon;
    private string _imageId;
    private string _imageReference;
    private string _instanceType;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, EmptyContainerCount);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon, new ContainersConfigurationReader().Read());
        var environment = new Ec2InstanceEnvironment(new IdentityConfigurationReader().Read(), new HostConfigurationReader().Read());
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, environment, writer),
            new DescribeInstancesOperation(store, writer),
            new StopInstancesOperation(store, containers, writer),
            new StartInstancesOperation(store, containers, writer),
            new RebootInstancesOperation(store, containers, writer),
            new TerminateInstancesOperation(store, containers, writer),
            new DescribeInstanceStatusOperation(store, containers, writer)
        };

        _imageId = configuration.AmiToImage.Keys.First();
        _imageReference = configuration.AmiToImage[_imageId];
        _instanceType = configuration.InstanceTypes.First().Name;
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Can_Run_An_Instance_From_The_Image_The_Ami_Names()
    {
        var request = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var instanceIds = document.Descendants()
            .Where(element => element.Name.LocalName == "instanceId")
            .Select(element => element.Value)
            .ToList();
        var imageId = document.Descendants().Single(element => element.Name.LocalName == "imageId").Value;
        var instanceType = document.Descendants().Single(element => element.Name.LocalName == "instanceType").Value;
        var stateCode = document.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var stateName = document.Descendants().Single(element => element.Name.LocalName == "name").Value;
        var privateIpAddress = document.Descendants().Single(element => element.Name.LocalName == "privateIpAddress").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(rootName, Is.EqualTo("RunInstancesResponse"));
        Assert.That(instanceIds, Has.Count.EqualTo(LaunchedInstanceCount));
        Assert.That(instanceIds.Single().StartsWith(InstanceIdPrefix), Is.True);
        Assert.That(imageId, Is.EqualTo(_imageId));
        Assert.That(instanceType, Is.EqualTo(_instanceType));
        Assert.That(stateCode, Is.EqualTo(StateCodeRunning));
        Assert.That(stateName, Is.EqualTo(StateNameRunning));
        Assert.That(privateIpAddress, Is.EqualTo(FirstContainerAddress));
        Assert.That(_daemon.LastStartedImageReference, Is.EqualTo(_imageReference));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_The_Running_Instance_A_Launch_Recorded()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var launchedReservationId = launchDocument.Descendants().Single(element => element.Name.LocalName == "reservationId").Value;

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstancesRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var reservationId = document.Descendants().Single(element => element.Name.LocalName == "reservationId").Value;
        var instanceId = document.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var imageId = document.Descendants().Single(element => element.Name.LocalName == "imageId").Value;
        var instanceType = document.Descendants().Single(element => element.Name.LocalName == "instanceType").Value;
        var stateName = document.Descendants().Single(element => element.Name.LocalName == "name").Value;
        var privateIpAddress = document.Descendants().Single(element => element.Name.LocalName == "privateIpAddress").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(rootName, Is.EqualTo("DescribeInstancesResponse"));
        Assert.That(reservationId, Is.EqualTo(launchedReservationId));
        Assert.That(instanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(imageId, Is.EqualTo(_imageId));
        Assert.That(instanceType, Is.EqualTo(_instanceType));
        Assert.That(stateName, Is.EqualTo(StateNameRunning));
        Assert.That(privateIpAddress, Is.EqualTo(FirstContainerAddress));
    }

    [Test]
    public async Task Test_That_We_Answer_Invalid_Ami_Id_Not_Found_When_The_Launch_Image_Is_Not_In_The_Catalogue()
    {
        var request = $"Action=RunInstances&ImageId={UnknownImageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == "Code").Value;
        var errorMessage = document.Descendants().Single(element => element.Name.LocalName == "Message").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo(InvalidImageIdNotFoundCode));
        Assert.That(errorMessage, Does.Contain(UnknownImageId));
        Assert.That(_daemon.LastStartedImageReference, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task Test_That_We_Can_Stop_A_Launched_Instance_And_Describe_It_As_Stopped()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        var stopRequest = string.Format(CultureInfo.InvariantCulture, StopInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(stopRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var stopDocument = XDocument.Load(_responseBody);
        var stopRootName = stopDocument.Elements().Single().Name.LocalName;
        var stoppedInstanceId = stopDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var stoppedStateCode = stopDocument.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var stoppedStateName = stopDocument.Descendants().Single(element => element.Name.LocalName == "name").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(stopRootName, Is.EqualTo(StopInstancesResponseRootName));
        Assert.That(stoppedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(stoppedStateCode, Is.EqualTo(StateCodeStopped));
        Assert.That(stoppedStateName, Is.EqualTo(StateNameStopped));
        Assert.That(_daemon.LastStoppedContainerId, Is.EqualTo(FirstContainerId));

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstancesRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var describedInstanceId = document.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var describedStateCode = document.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var describedStateName = document.Descendants().Single(element => element.Name.LocalName == "name").Value;

        Assert.That(describedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(describedStateCode, Is.EqualTo(StateCodeStopped));
        Assert.That(describedStateName, Is.EqualTo(StateNameStopped));
    }

    [Test]
    public async Task Test_That_We_Can_Start_The_Stopped_Instance_And_Report_Its_Instance_Id_Running()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        var stopRequest = string.Format(CultureInfo.InvariantCulture, StopInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(stopRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        var startRequest = string.Format(CultureInfo.InvariantCulture, StartInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(startRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var startDocument = XDocument.Load(_responseBody);
        var startRootName = startDocument.Elements().Single().Name.LocalName;
        var startedInstanceId = startDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var startedStateCode = startDocument.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var startedStateName = startDocument.Descendants().Single(element => element.Name.LocalName == "name").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(startRootName, Is.EqualTo(StartInstancesResponseRootName));
        Assert.That(startedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(startedStateCode, Is.EqualTo(StateCodeRunning));
        Assert.That(startedStateName, Is.EqualTo(StateNameRunning));
        Assert.That(_daemon.LastStartedExistingContainerId, Is.EqualTo(FirstContainerId));
    }

    [Test]
    public async Task Test_That_We_Can_Terminate_A_Launched_Instance_Removing_Its_Container_And_Describe_It_As_Terminated()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        var terminateRequest = string.Format(CultureInfo.InvariantCulture, TerminateInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(terminateRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var terminateDocument = XDocument.Load(_responseBody);
        var terminateRootName = terminateDocument.Elements().Single().Name.LocalName;
        var terminatedInstanceId = terminateDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var terminatedStateCode = terminateDocument.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var terminatedStateName = terminateDocument.Descendants().Single(element => element.Name.LocalName == "name").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(terminateRootName, Is.EqualTo(TerminateInstancesResponseRootName));
        Assert.That(terminatedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(terminatedStateCode, Is.EqualTo(StateCodeTerminated));
        Assert.That(terminatedStateName, Is.EqualTo(StateNameTerminated));
        Assert.That(_daemon.LastRemovedContainerId, Is.EqualTo(FirstContainerId));

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstancesRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var describedInstanceId = document.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var describedStateCode = document.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var describedStateName = document.Descendants().Single(element => element.Name.LocalName == "name").Value;

        Assert.That(describedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(describedStateCode, Is.EqualTo(StateCodeTerminated));
        Assert.That(describedStateName, Is.EqualTo(StateNameTerminated));
    }

    [Test]
    public async Task Test_That_We_Describe_The_Instance_Status_Ok_While_The_Container_Runs_And_Not_Ok_Once_It_Is_Stopped()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstanceStatusRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var runningDocument = XDocument.Load(_responseBody);
        var runningRootName = runningDocument.Elements().Single().Name.LocalName;
        var runningInstanceId = runningDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var runningStatusName = runningDocument.Descendants()
            .Single(element => element.Name.LocalName == "instanceStatus")
            .Elements()
            .Single(element => element.Name.LocalName == "status")
            .Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(runningRootName, Is.EqualTo(DescribeInstanceStatusResponseRootName));
        Assert.That(runningInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(runningStatusName, Is.EqualTo(StatusOk));

        var stopRequest = string.Format(CultureInfo.InvariantCulture, StopInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(stopRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstanceStatusRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var stoppedDocument = XDocument.Load(_responseBody);
        var stoppedInstanceId = stoppedDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var stoppedStatusName = stoppedDocument.Descendants()
            .Single(element => element.Name.LocalName == "instanceStatus")
            .Elements()
            .Single(element => element.Name.LocalName == "status")
            .Value;

        Assert.That(stoppedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(stoppedStatusName, Is.EqualTo(StatusNotOk));
    }

    [Test]
    public async Task Test_That_We_Reboot_A_Launched_Instance_Restarting_Its_Container_And_Report_It_Running_With_An_Increased_Boot_Count()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var launchedBootCount = launchDocument.Descendants().Single(element => element.Name.LocalName == "bootCount").Value;

        var rebootRequest = string.Format(CultureInfo.InvariantCulture, RebootInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rebootRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var rebootDocument = XDocument.Load(_responseBody);
        var rebootRootName = rebootDocument.Elements().Single().Name.LocalName;
        var rebootedInstanceId = rebootDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var rebootedStateCode = rebootDocument.Descendants().Single(element => element.Name.LocalName == "code").Value;
        var rebootedStateName = rebootDocument.Descendants().Single(element => element.Name.LocalName == "name").Value;
        var rebootedBootCount = rebootDocument.Descendants().Single(element => element.Name.LocalName == "bootCount").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(launchedBootCount, Is.EqualTo(FirstBootCount));
        Assert.That(rebootRootName, Is.EqualTo(RebootInstancesResponseRootName));
        Assert.That(rebootedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(rebootedStateCode, Is.EqualTo(StateCodeRunning));
        Assert.That(rebootedStateName, Is.EqualTo(StateNameRunning));
        Assert.That(rebootedBootCount, Is.EqualTo(SecondBootCount));
        Assert.That(_daemon.LastRestartedContainerId, Is.EqualTo(FirstContainerId));
        Assert.That(_daemon.StartedContainerCount, Is.EqualTo(LaunchedInstanceCount));
        Assert.That(_daemon.LastStoppedContainerId, Is.EqualTo(string.Empty));
        Assert.That(_daemon.LastRemovedContainerId, Is.EqualTo(string.Empty));

        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeInstancesRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var describedInstanceId = document.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;
        var describedStateName = document.Descendants().Single(element => element.Name.LocalName == "name").Value;
        var describedBootCount = document.Descendants().Single(element => element.Name.LocalName == "bootCount").Value;

        Assert.That(describedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(describedStateName, Is.EqualTo(StateNameRunning));
        Assert.That(describedBootCount, Is.EqualTo(SecondBootCount));
    }

    [Test]
    public async Task Test_That_We_Answer_A_Reboot_Request_Arriving_From_Inside_The_Guest_Through_The_Same_Restart()
    {
        var launchRequest = $"Action=RunInstances&ImageId={_imageId}&InstanceType={_instanceType}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        var rebootRequest = string.Format(CultureInfo.InvariantCulture, RebootInstancesRequestFormat, launchedInstanceId);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Connection.RemoteIpAddress = IPAddress.Parse(FirstContainerAddress);
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rebootRequest));
        _context.Response.Body = _responseBody;

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var rebootDocument = XDocument.Load(_responseBody);
        var rebootRootName = rebootDocument.Elements().Single().Name.LocalName;
        var rebootedInstanceId = rebootDocument.Descendants().Single(element => element.Name.LocalName == "instanceId").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(rebootRootName, Is.EqualTo(RebootInstancesResponseRootName));
        Assert.That(rebootedInstanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(_daemon.LastRestartedContainerId, Is.EqualTo(FirstContainerId));
        Assert.That(_daemon.StartedContainerCount, Is.EqualTo(LaunchedInstanceCount));
        Assert.That(_daemon.LastRemovedContainerId, Is.EqualTo(string.Empty));
    }
}
