namespace AwsEmulatorSharp.Emulator.Pipeline;

public abstract class PipelineStep
{
    protected PipelineStep(string name, PipelineConfiguration configuration)
    {
        Name = name;
        Configuration = configuration;
    }

    public string Name { get; }

    public PipelineConfiguration Configuration { get; }
}
