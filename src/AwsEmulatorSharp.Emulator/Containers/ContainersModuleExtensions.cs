using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Containers;

public static class ContainersModuleExtensions
{
    public static IServiceCollection AddContainersModule(this IServiceCollection services)
    {
        var configurationReader = new ContainersConfigurationReader();
        var configuration = configurationReader.Read();

        services.AddSingleton(configuration);
        services.AddSingleton<IContainerDaemon, DockerContainerDaemon>();
        services.AddSingleton<ContainersSurface>();

        return services;
    }
}
