using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Ec2.Recovery;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Ec2.Recovery;

[TimedTestFixture(1)]
public class InstanceRecoveryTests
{
    private const string AmiLabelKey = "ami-id";
    private const string InstanceTypeLabelKey = "instance-type";
    private const string ServedContainerId = "container-1";
    private const string OrphanContainerId = "container-2";
    private const string OrphanImageId = "ami-00000000000000000";
    private const string RunningStateName = "running";
    private const int RunningStateCode = 16;
    private const int SuccessfulExitCode = 0;
    private const int RecoveredInstanceCount = 1;

    private InMemoryContainerDaemon _daemon;
    private InstanceRecovery _recovery;
    private string _imageId;
    private string _instanceType;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        _imageId = configuration.AmiToImage.Keys.First();
        _instanceType = configuration.InstanceTypes.First().Name;
        var commandResult = new ContainerCommandResult(string.Empty, string.Empty, SuccessfulExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        var containers = new ContainersSurface(_daemon);
        var servedLabels = new Dictionary<string, string>
        {
            [AmiLabelKey] = _imageId,
            [InstanceTypeLabelKey] = _instanceType
        };
        containers.Start(configuration.AmiToImage[_imageId], servedLabels);
        var orphanLabels = new Dictionary<string, string>
        {
            [AmiLabelKey] = OrphanImageId,
            [InstanceTypeLabelKey] = _instanceType
        };
        containers.Start(configuration.AmiToImage[_imageId], orphanLabels);
        _recovery = new InstanceRecovery(configuration, new InstanceStore(), containers);
    }

    [Test]
    public void Test_That_Startup_Over_A_Labelled_Container_Rebuilds_A_Record_Holding_Its_Container_Id_And_Reports_The_Instance_Running()
    {
        var recoveredInstances = _recovery.Recover();

        Assert.That(recoveredInstances, Has.Count.EqualTo(RecoveredInstanceCount));
        Assert.That(recoveredInstances.Single().ContainerId, Is.EqualTo(ServedContainerId));
        Assert.That(recoveredInstances.Single().ImageId, Is.EqualTo(_imageId));
        Assert.That(recoveredInstances.Single().InstanceType, Is.EqualTo(_instanceType));
        Assert.That(recoveredInstances.Single().StateName, Is.EqualTo(RunningStateName));
        Assert.That(recoveredInstances.Single().StateCode, Is.EqualTo(RunningStateCode));
    }

    [Test]
    public void Test_That_Startup_Removes_A_Labelled_Container_That_No_Record_Names()
    {
        var recoveredInstances = _recovery.Recover();

        var orphanedInstance = recoveredInstances.SingleOrDefault(instance => instance.ContainerId == OrphanContainerId);
        Assert.That(_daemon.LastRemovedContainerId, Is.EqualTo(OrphanContainerId));
        Assert.That(orphanedInstance, Is.Null);
    }
}
