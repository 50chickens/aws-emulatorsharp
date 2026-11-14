using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Secrets;

public static class SecretsModuleExtensions
{
    public static IServiceCollection AddSecretsModule(this IServiceCollection services)
    {
        services.AddSingleton<SecretStore>();
        services.AddSingleton<SecretsResponseWriter>();
        services.AddSingleton<SecretsSurface>();

        return services;
    }
}
