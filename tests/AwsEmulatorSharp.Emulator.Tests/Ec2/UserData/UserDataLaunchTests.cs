using System.Diagnostics;
using System.Globalization;
using System.Text;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.UserData;

[TimedTestFixture(1)]
public class UserDataLaunchTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string RunInstancesRequestFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}&UserData={2}";
    private const string RunInstancesRequestWithoutUserDataFormat = "Action=RunInstances&ImageId={0}&InstanceType={1}";
    private const string PowerShellExecutable = "powershell.exe";
    private const string CommandFlag = "-Command";
    private const string NoProfileFlag = "-NoProfile";
    private const string SetContentCommandFormat = "Set-Content -Path {0} -Value {1}";
    private const string CommentLine = "# cloud-init user data";
    private const string UserDataFileName = "user-data-proof.txt";
    private const string UserDataProofValue = "user-data-ran";
    private const string FirstContainerId = "container-1";
    private const int SuccessfulExitCode = 0;
    private const int EmptyCommandArgumentCount = 0;
    private const int PowerShellCommandArgumentCount = 6;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private InMemoryContainerDaemon _daemon;
    private string _imageId;
    private string _instanceType;
    private string _script;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        var store = new InstanceStore();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, SuccessfulExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon);
        var operations = new List<IQueryOperation>
        {
            new RunInstancesOperation(configuration, store, containers, writer)
        };

        _imageId = configuration.AmiToImage.Keys.First();
        _instanceType = configuration.InstanceTypes.First().Name;
        _script = string.Join(
            System.Environment.NewLine,
            CommentLine,
            string.Format(CultureInfo.InvariantCulture, SetContentCommandFormat, UserDataFileName, UserDataProofValue));
        _surface = new WireSurface(operations, writer, new RecordingLogger());
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Run_The_UserData_Script_Under_PowerShell_In_The_Instance_Container()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesRequestFormat,
            _imageId,
            _instanceType,
            Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(_script))));
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));
        await _surface.HandleAsync(_context);

        var recordedCommand = _daemon.LastCommand;
        var executable = recordedCommand.First();
        var script = recordedCommand.Last();

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(recordedCommand, Has.Count.EqualTo(PowerShellCommandArgumentCount));
        Assert.That(executable, Is.EqualTo(PowerShellExecutable));
        Assert.That(recordedCommand, Does.Contain(CommandFlag));
        Assert.That(script, Is.EqualTo(_script));
        Assert.That(_daemon.LastCommandContainerId, Is.EqualTo(FirstContainerId));
    }

    [Test]
    public async Task Test_That_The_File_The_UserData_Script_Writes_Is_Written_Under_PowerShell()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesRequestFormat,
            _imageId,
            _instanceType,
            Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(_script))));
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));
        await _surface.HandleAsync(_context);

        var recordedCommand = _daemon.LastCommand;
        var startInfo = new ProcessStartInfo(recordedCommand.First())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        var arguments = recordedCommand.Skip(1).ToList();
        arguments.ForEach(startInfo.ArgumentList.Add);

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        var filePath = Path.Combine(AppContext.BaseDirectory, UserDataFileName);
        var fileContents = File.ReadAllText(filePath);
        File.Delete(filePath);

        Assert.That(process.ExitCode, Is.EqualTo(SuccessfulExitCode));
        Assert.That(fileContents, Does.Contain(UserDataProofValue));
    }

    [Test]
    public async Task Test_That_We_Send_No_Command_To_The_Container_When_The_Launch_Carries_No_UserData()
    {
        var request = string.Format(
            CultureInfo.InvariantCulture,
            RunInstancesRequestWithoutUserDataFormat,
            _imageId,
            _instanceType);
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        Assert.That(_daemon.LastCommand, Has.Count.EqualTo(EmptyCommandArgumentCount));
    }
}
