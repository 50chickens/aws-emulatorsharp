using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
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
    private const string InstanceIdPrefix = "i-";
    private const string StateCodeRunning = "16";
    private const string StateNameRunning = "running";
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
        var containers = new ContainersSurface(_daemon);
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, writer),
            new DescribeInstancesOperation(store, containers, writer)
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
}
