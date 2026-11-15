namespace AwsEmulatorSharp.Emulator.Pipeline;

public class Pipeline
{
    public Pipeline(PipelineConfiguration configuration)
    {
        Configuration = configuration;
        Steps = new List<PipelineStep>
        {
            new CreateBucketStep(configuration),
            new UploadMediaStep(configuration),
            new WriteSecretsStep(configuration),
            new LaunchStep(configuration),
            new AwaitRunningStep(configuration),
            new AwaitUserDataStep(configuration),
            new AssertInstallStep(configuration),
            new TerminateStep(configuration)
        };
    }

    public PipelineConfiguration Configuration { get; }

    public IReadOnlyList<PipelineStep> Steps { get; }
}
