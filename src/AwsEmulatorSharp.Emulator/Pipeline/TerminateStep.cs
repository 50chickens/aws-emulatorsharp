namespace AwsEmulatorSharp.Emulator.Pipeline;

public class TerminateStep : PipelineStep
{
    private const string StepName = "terminate";

    public TerminateStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }
}
