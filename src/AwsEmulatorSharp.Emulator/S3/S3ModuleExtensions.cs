using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.S3;

public static class S3ModuleExtensions
{
    public static IServiceCollection AddS3Module(this IServiceCollection services)
    {
        return services;
    }
}
