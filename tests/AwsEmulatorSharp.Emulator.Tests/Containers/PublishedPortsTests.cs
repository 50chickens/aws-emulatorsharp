using System.Globalization;
using System.Text;
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

namespace AwsEmulatorSharp.Emulator.Tests;

[TimedTestFixture(1)]
public class PublishedPortsTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}";
    private const int SuccessfulExitCode = 0;

    private ContainersConfiguration _configuration;
    private InMemoryContainerDaemon _daemon;
    private WireSurface _surface;
    private DefaultHttpContext _context;

    [SetUp]
    public void SetUp()
    {
        _configuration = new ContainersConfigurationReader().Read();
        var ec2Configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, SuccessfulExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon, _configuration);
        var environment = new Ec2InstanceEnvironment(new IdentityConfigurationReader().Read(), new HostConfigurationReader().Read());
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(ec2Configuration, store, containers, environment, writer)
        };
        var imageId = ec2Configuration.AmiToImage.Keys.First();
        var instanceType = ec2Configuration.InstanceTypes.First().Name;
        var request = string.Format(CultureInfo.InvariantCulture, RunInstancesRequestFormat, imageId, instanceType);

        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));
        _context.Response.Body = new MemoryStream();
    }

    [Test]
    public async Task Test_That_A_Launch_Publishes_Every_Host_Port_The_Containers_Configuration_Declares()
    {
        await _surface.HandleAsync(_context);

        Assert.That(_daemon.LastPublishedPorts, Is.EqualTo(_configuration.PublishedPorts));
    }
}
