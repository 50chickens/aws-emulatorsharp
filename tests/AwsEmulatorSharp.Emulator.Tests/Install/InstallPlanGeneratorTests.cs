using System.Globalization;
using System.Text;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Install;
using AwsEmulatorSharp.Emulator.S3;
using AwsEmulatorSharp.Emulator.Tests.Identity;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Install;

[TimedTestFixture(1)]
public class InstallPlanGeneratorTests
{
    private const string MediaFileName = "sql-server-setup.exe";
    private const string OlderMediaFileName = "sql-server-setup-2025.exe";
    private const string MediaContentText = "installer-media";
    private const string InstanceId = "i-0123456789abcdef0";
    private const string ReservationId = "r-0123456789abcdef0";
    private const string ImageId = "ami-0123456789abcdef0";
    private const string InstanceType = "m5.large";
    private const string ContainerId = "container-1";
    private const string PrivateIpAddress = "10.0.0.4";
    private const string StateName = "running";
    private const string ConsoleOutput = "";
    private const string SigningDate = "20261115";
    private const string SigningTimestamp = "20261115T104209Z";
    private const string CredentialFormat = "{0}/{1}/{2}/s3/aws4_request";
    private const string PresignedUrlFormat = "{0}/{1}?X-Amz-Algorithm=AWS4-HMAC-SHA256";
    private const string EndpointFormat = "http://{0}:{1}/";
    private const string ExpectedCommandLineFormat = "\"{0}\\{1}\" /Q /ACTION=Install /FEATURES=IS /IACCEPTSQLSERVERLICENSETERMS";
    private const string SignatureQueryName = "X-Amz-Signature=";
    private const string ExpectedSignature = "cc32958840dae4d681c3e35b12c157c5f44805ee22120cf5d272ef6c7abcd8ff";
    private const int StateCode = 16;
    private const int BootCount = 1;
    private const bool UserDataFailed = false;

    private static readonly DateTimeOffset _signingTime = new(2026, 11, 15, 10, 42, 9, TimeSpan.Zero);

    private HostConfiguration _hostConfiguration;
    private Ec2Configuration _ec2Configuration;
    private InstallConfiguration _configuration;
    private Ec2Instance _instance;
    private InstallPlanGenerator _generator;
    private string _mediaKey;

    [SetUp]
    public void SetUp()
    {
        _hostConfiguration = new HostConfigurationReader().Read();
        _ec2Configuration = new Ec2ConfigurationReader().Read();
        _configuration = new InstallConfigurationReader().Read();
        _mediaKey = _configuration.MediaKeyPrefix + MediaFileName;
        var olderMediaKey = _configuration.MediaKeyPrefix + OlderMediaFileName;
        var objectStore = new ObjectStore();
        objectStore.PutObject(_configuration.MediaBucketName, olderMediaKey, Encoding.UTF8.GetBytes(MediaContentText));
        objectStore.PutObject(_configuration.MediaBucketName, _mediaKey, Encoding.UTF8.GetBytes(MediaContentText));
        var timeProvider = new FixedTimeProvider(_signingTime);
        var mediaUrlMinter = new MediaUrlMinter(_hostConfiguration, _ec2Configuration, timeProvider);

        _generator = new InstallPlanGenerator(objectStore, mediaUrlMinter, _configuration);
        _instance = new Ec2Instance(InstanceId, ReservationId, ImageId, InstanceType, ContainerId, PrivateIpAddress, StateName, StateCode, BootCount, ConsoleOutput, UserDataFailed);
    }

    [Test]
    public void Test_That_The_Plan_For_An_Instance_Is_For_That_Instance()
    {
        var plan = _generator.For(_instance);

        Assert.That(plan.InstanceId, Is.EqualTo(_instance.InstanceId));
    }

    [Test]
    public void Test_That_The_Plan_Names_The_Newest_Media_Object_The_Object_Store_Holds()
    {
        var plan = _generator.For(_instance);

        Assert.That(plan.MediaBucketName, Is.EqualTo(_configuration.MediaBucketName));
        Assert.That(plan.MediaKey, Is.EqualTo(_mediaKey));
    }

    [Test]
    public void Test_That_The_Presigned_Media_Url_Addresses_The_Media_Object_From_The_Container_Endpoint()
    {
        var plan = _generator.For(_instance);
        var endpoint = string.Format(CultureInfo.InvariantCulture, EndpointFormat, _hostConfiguration.ContainerHostName, _hostConfiguration.ListenerPort);
        var presignedPath = string.Format(CultureInfo.InvariantCulture, PresignedUrlFormat, _configuration.MediaBucketName, _mediaKey);

        Assert.That(plan.MediaUrl, Does.Contain(endpoint));
        Assert.That(plan.MediaUrl, Does.Contain(presignedPath));
    }

    [Test]
    public void Test_That_The_Presigned_Media_Url_Is_Signed_With_The_Emulator_Credential()
    {
        var plan = _generator.For(_instance);
        var credential = string.Format(CultureInfo.InvariantCulture, CredentialFormat, _hostConfiguration.Credentials.AccessKeyId, SigningDate, _ec2Configuration.Region.Name);

        Assert.That(plan.MediaUrl, Does.Contain(Uri.EscapeDataString(credential)));
        Assert.That(plan.MediaUrl, Does.Contain(SigningTimestamp));
        Assert.That(plan.MediaUrl, Does.Contain(SignatureQueryName + ExpectedSignature));
    }

    [Test]
    public void Test_That_The_Plan_Carries_An_Unattended_Setup_Command_Line_With_The_Integration_Services_Feature()
    {
        var plan = _generator.For(_instance);
        var expectedCommandLine = string.Format(CultureInfo.InvariantCulture, ExpectedCommandLineFormat, _configuration.MediaFolderPath, MediaFileName);

        Assert.That(plan.SetupCommandLine, Is.EqualTo(expectedCommandLine));
    }
}
