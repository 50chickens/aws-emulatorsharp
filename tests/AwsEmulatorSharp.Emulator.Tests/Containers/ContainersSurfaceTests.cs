using AwsEmulatorSharp.Emulator.Containers;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests;

[TimedTestFixture(1)]
public class ContainersSurfaceTests
{
    private const string ImageReference = "mcr.microsoft.com/dotnet/framework/runtime:4.8.1-windowsservercore-ltsc2025";
    private const string LabelKey = "emulator-task";
    private const string LabelValue = "task-1";
    private const string CommandName = "echo";
    private const string CommandArgument = "hello";
    private const string FirstContainerAddress = "10.0.0.1";
    private const string StandardOutput = "hello";
    private const string StandardError = "warning";
    private const int ExitCode = 3;

    private Dictionary<string, string> _labels;
    private List<string> _command;
    private InMemoryContainerDaemon _daemon;
    private ContainersSurface _surface;

    [SetUp]
    public void SetUp()
    {
        _labels = new Dictionary<string, string> { [LabelKey] = LabelValue };
        _command = new List<string> { CommandName, CommandArgument };
        var commandResult = new ContainerCommandResult(StandardOutput, StandardError, ExitCode);
        _daemon = new InMemoryContainerDaemon(commandResult);
        _surface = new ContainersSurface(_daemon);
    }

    [Test]
    public void Test_That_We_Can_Start_A_Container_From_An_Image_Reference_And_Report_It_Running()
    {
        var container = _surface.Start(ImageReference, _labels);

        Assert.That(container.State, Is.EqualTo(ContainerState.Running));
        Assert.That(container.Address, Is.EqualTo(FirstContainerAddress));
    }

    [Test]
    public void Test_That_We_Can_Run_A_Command_In_A_Container_And_Get_Standard_Output_Standard_Error_And_Exit_Code()
    {
        var container = _surface.Start(ImageReference, _labels);

        var commandResult = _surface.RunCommand(container.ContainerId, _command);

        Assert.That(commandResult.StandardOutput, Is.EqualTo(StandardOutput));
        Assert.That(commandResult.StandardError, Is.EqualTo(StandardError));
        Assert.That(commandResult.ExitCode, Is.EqualTo(ExitCode));
    }

    [Test]
    public void Test_That_We_Can_Find_A_Labelled_Container_By_Its_Label_After_A_Restart()
    {
        var container = _surface.Start(ImageReference, _labels);
        var restartedSurface = new ContainersSurface(_daemon);

        var containers = restartedSurface.FindByLabel(LabelKey, LabelValue);

        Assert.That(containers, Has.Count.EqualTo(1));
        Assert.That(containers.First().ContainerId, Is.EqualTo(container.ContainerId));
    }

    [Test]
    public void Test_That_We_Can_List_Nothing_For_A_Removed_Container()
    {
        var container = _surface.Start(ImageReference, _labels);

        _surface.Remove(container.ContainerId);

        var containers = _surface.FindByLabel(LabelKey, LabelValue);

        Assert.That(containers, Has.Count.EqualTo(0));
    }

    [Test]
    public void Test_That_We_Can_Find_A_Labelled_Container_By_Its_Label_Key_After_A_Restart()
    {
        var container = _surface.Start(ImageReference, _labels);
        var restartedSurface = new ContainersSurface(_daemon);

        var containers = restartedSurface.FindByLabel(LabelKey);

        Assert.That(containers, Has.Count.EqualTo(1));
        Assert.That(containers.Single().ContainerId, Is.EqualTo(container.ContainerId));
    }

    [Test]
    public void Test_That_We_Can_Read_The_Labels_A_Container_Carries_After_A_Restart()
    {
        var container = _surface.Start(ImageReference, _labels);
        var restartedSurface = new ContainersSurface(_daemon);

        var labels = restartedSurface.Labels(container.ContainerId);

        Assert.That(labels[LabelKey], Is.EqualTo(LabelValue));
    }
}
