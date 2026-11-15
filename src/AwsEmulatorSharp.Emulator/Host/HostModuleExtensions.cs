using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Host;

public static class HostModuleExtensions
{
    public static IServiceCollection AddHostModule(this IServiceCollection services)
    {
        var configurationReader = new HostConfigurationReader();
        var configuration = configurationReader.Read();

        services.AddSingleton(configuration);

        return services;
    }
}
