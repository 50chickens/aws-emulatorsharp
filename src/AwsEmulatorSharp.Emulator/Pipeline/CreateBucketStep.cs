namespace AwsEmulatorSharp.Emulator.Pipeline;

public class CreateBucketStep : PipelineStep
{
    private const string StepName = "create-bucket";

    public CreateBucketStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public string BucketName => Configuration.Install.MediaBucketName;
}
