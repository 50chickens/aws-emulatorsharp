namespace AwsEmulatorSharp.Emulator.Pipeline;

public class AwaitRunningStep : PipelineStep
{
    private const string StepName = "await-running";

    public AwaitRunningStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public TimeSpan Timeout => Configuration.AwaitTimeout;
}
