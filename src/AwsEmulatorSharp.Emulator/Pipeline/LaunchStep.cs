namespace AwsEmulatorSharp.Emulator.Pipeline;

public class LaunchStep : PipelineStep
{
    private const string StepName = "launch";

    public LaunchStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public string ImageId => Configuration.ImageId;

    public string InstanceType => Configuration.InstanceType;

    public string UserDataScript => Configuration.UserDataScript;
}
