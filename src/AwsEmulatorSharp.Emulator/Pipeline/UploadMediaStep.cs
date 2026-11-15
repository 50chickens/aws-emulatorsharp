namespace AwsEmulatorSharp.Emulator.Pipeline;

public class UploadMediaStep : PipelineStep
{
    private const string StepName = "upload-media";

    public UploadMediaStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public string BucketName => Configuration.Install.MediaBucketName;

    public string MediaKeyPrefix => Configuration.Install.MediaKeyPrefix;

    public string MediaFolderPath => Configuration.Install.MediaFolderPath;
}
