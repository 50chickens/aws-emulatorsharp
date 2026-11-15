using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Install;
using AwsEmulatorSharp.Emulator.Pipeline;
using FiftyChickens.Library.Testing;
using NUnit.Framework;
using PipelineDefinition = AwsEmulatorSharp.Emulator.Pipeline.Pipeline;

namespace AwsEmulatorSharp.Emulator.Tests.Pipeline;

[TimedTestFixture(1)]
public class PipelineTests
{
    private const string CreateBucketStepName = "create-bucket";
    private const string UploadMediaStepName = "upload-media";
    private const string WriteSecretsStepName = "write-secrets";
    private const string LaunchStepName = "launch";
    private const string AwaitRunningStepName = "await-running";
    private const string AwaitUserDataStepName = "await-user-data";
    private const string AssertInstallStepName = "assert-install";
    private const string TerminateStepName = "terminate";
    private const string SecretName = "report-server-credential";
    private const string SecretString = "pipeline-secret-value";
    private const string UserDataScript = "Install-WindowsFeature -Name Web-Server";
    private const int AwaitTimeoutSeconds = 900;

    private PipelineConfiguration _configuration;
    private PipelineDefinition _pipeline;

    [SetUp]
    public void SetUp()
    {
        var installConfiguration = new InstallConfigurationReader().Read();
        var ec2Configuration = new Ec2ConfigurationReader().Read();
        _configuration = new PipelineConfiguration
        {
            Install = installConfiguration,
            SecretName = SecretName,
            SecretString = SecretString,
            ImageId = ec2Configuration.AmiToImage.Keys.First(),
            InstanceType = ec2Configuration.InstanceTypes.First().Name,
            UserDataScript = UserDataScript,
            AwaitTimeout = TimeSpan.FromSeconds(AwaitTimeoutSeconds)
        };
        _pipeline = new PipelineDefinition(_configuration);
    }

    [Test]
    public void Test_That_The_Pipeline_Carries_The_Steps_In_Order()
    {
        var stepNames = _pipeline.Steps.Select(step => step.Name).ToList();
        var expectedStepNames = new List<string>
        {
            CreateBucketStepName,
            UploadMediaStepName,
            WriteSecretsStepName,
            LaunchStepName,
            AwaitRunningStepName,
            AwaitUserDataStepName,
            AssertInstallStepName,
            TerminateStepName
        };

        Assert.That(stepNames, Is.EqualTo(expectedStepNames));
    }

    [Test]
    public void Test_That_The_Create_Bucket_Step_Is_Given_The_Media_Bucket_Of_The_Configuration()
    {
        var step = _pipeline.Steps.OfType<CreateBucketStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.BucketName, Is.EqualTo(_configuration.Install.MediaBucketName));
    }

    [Test]
    public void Test_That_The_Upload_Media_Step_Is_Given_The_Media_The_Configuration_Names()
    {
        var step = _pipeline.Steps.OfType<UploadMediaStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.BucketName, Is.EqualTo(_configuration.Install.MediaBucketName));
        Assert.That(step.MediaKeyPrefix, Is.EqualTo(_configuration.Install.MediaKeyPrefix));
        Assert.That(step.MediaFolderPath, Is.EqualTo(_configuration.Install.MediaFolderPath));
    }

    [Test]
    public void Test_That_The_Write_Secrets_Step_Is_Given_The_Secret_The_Configuration_Names()
    {
        var step = _pipeline.Steps.OfType<WriteSecretsStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.SecretName, Is.EqualTo(_configuration.SecretName));
        Assert.That(step.SecretString, Is.EqualTo(_configuration.SecretString));
    }

    [Test]
    public void Test_That_The_Launch_Step_Is_Given_The_Image_The_Instance_Type_And_The_User_Data_Of_The_Configuration()
    {
        var step = _pipeline.Steps.OfType<LaunchStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.ImageId, Is.EqualTo(_configuration.ImageId));
        Assert.That(step.InstanceType, Is.EqualTo(_configuration.InstanceType));
        Assert.That(step.UserDataScript, Is.EqualTo(_configuration.UserDataScript));
    }

    [Test]
    public void Test_That_The_Await_Running_Step_Is_Given_The_Await_Timeout_Of_The_Configuration()
    {
        var step = _pipeline.Steps.OfType<AwaitRunningStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.Timeout, Is.EqualTo(_configuration.AwaitTimeout));
    }

    [Test]
    public void Test_That_The_Await_User_Data_Step_Is_Given_The_Await_Timeout_Of_The_Configuration()
    {
        var step = _pipeline.Steps.OfType<AwaitUserDataStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.Timeout, Is.EqualTo(_configuration.AwaitTimeout));
    }

    [Test]
    public void Test_That_The_Assert_Install_Step_Is_Given_The_Install_Configuration()
    {
        var step = _pipeline.Steps.OfType<AssertInstallStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
        Assert.That(step.Install, Is.EqualTo(_configuration.Install));
    }

    [Test]
    public void Test_That_The_Terminate_Step_Is_Given_The_One_Configuration_Object()
    {
        var step = _pipeline.Steps.OfType<TerminateStep>().Single();

        Assert.That(step.Configuration, Is.EqualTo(_configuration));
    }
}
