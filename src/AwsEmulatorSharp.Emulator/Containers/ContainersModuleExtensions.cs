using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Containers;

public static class ContainersModuleExtensions
{
    public static IServiceCollection AddContainersModule(this IServiceCollection services)
    {
        return services;
    }
}
