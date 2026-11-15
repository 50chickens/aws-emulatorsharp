using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Ec2.UserData;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.UserData;

[TimedTestFixture(1)]
public class GetConsoleOutputTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&UserData={2}";
    private const string RunInstancesRequestWithoutUserDataFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}";
    private const string GetConsoleOutputRequestFormat = "Action=GetConsoleOutput&InstanceId={0}";
    private const string GetConsoleOutputResponseRootName = "GetConsoleOutputResponse";
    private const string InstanceIdElementName = "instanceId";
    private const string OutputElementName = "output";
    private const string ScriptOutputLine = "Write-Output 'user-data-ran'";
    private const string StandardOutput = "user-data-ran";
    private const string StandardError = "user-data-warning";
    private const string EmptyOutput = "";
    private const int SuccessfulExitCode = 0;

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
        var commandResult = new ContainerCommandResult(StandardOutput, StandardError, SuccessfulExitCode);
        var daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(daemon);
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, writer),
            new GetConsoleOutputOperation(store, writer)
        };

        _imageId = configuration.AmiToImage.Keys.First();
        _instanceType = configuration.InstanceTypes.First().Name;
        _encodedUserData = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(ScriptOutputLine)));
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Return_The_Standard_Output_The_UserData_Produced()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, _imageId, _instanceType, _encodedUserData);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));
        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().First(element => element.Name.LocalName == InstanceIdElementName).Value;

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
        var instanceId = document.Descendants().Single(element => element.Name.LocalName == InstanceIdElementName).Value;
        var encodedOutput = document.Descendants().Single(element => element.Name.LocalName == OutputElementName).Value;
        var consoleOutput = Encoding.UTF8.GetString(Convert.FromBase64String(encodedOutput));

        Assert.That(rootName, Is.EqualTo(GetConsoleOutputResponseRootName));
        Assert.That(instanceId, Is.EqualTo(launchedInstanceId));
        Assert.That(consoleOutput, Does.Contain(StandardOutput));
    }

    [Test]
    public async Task Test_That_We_Return_Empty_Output_For_An_Instance_Whose_UserData_Has_Not_Run()
    {
        var launchRequest = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestWithoutUserDataFormat, _imageId, _instanceType);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(launchRequest));
        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var launchDocument = XDocument.Load(_responseBody);
        var launchedInstanceId = launchDocument.Descendants().First(element => element.Name.LocalName == InstanceIdElementName).Value;

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
        Assert.That(consoleOutput, Is.EqualTo(EmptyOutput));
    }
}
