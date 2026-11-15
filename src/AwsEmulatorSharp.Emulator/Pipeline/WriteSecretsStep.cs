namespace AwsEmulatorSharp.Emulator.Pipeline;

public class WriteSecretsStep : PipelineStep
{
    private const string StepName = "write-secrets";

    public WriteSecretsStep(PipelineConfiguration configuration)
        : base(StepName, configuration)
    {
    }

    public string SecretName => Configuration.SecretName;

    public string SecretString => Configuration.SecretString;
}
