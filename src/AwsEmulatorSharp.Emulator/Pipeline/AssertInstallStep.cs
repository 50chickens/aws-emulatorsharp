using AwsEmulatorSharp.Emulator.Install;

namespace AwsEmulatorSharp.Emulator.Pipeline;

public class AssertInstallStep : PipelineStep
{
    private const string StepName = "assert-install";

    public AssertInstallStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public InstallConfiguration Install => Configuration.Install;
}
