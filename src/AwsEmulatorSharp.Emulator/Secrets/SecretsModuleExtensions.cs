using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Secrets;

public static class SecretsModuleExtensions
{
    public static IServiceCollection AddSecretsModule(this IServiceCollection services)
    {
        return services;
    }
}
