namespace AwsEmulatorSharp.Emulator.Pipeline;

public class AwaitUserDataStep : PipelineStep
{
    private const string StepName = "await-user-data";

    public AwaitUserDataStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public TimeSpan Timeout => Configuration.AwaitTimeout;
}
