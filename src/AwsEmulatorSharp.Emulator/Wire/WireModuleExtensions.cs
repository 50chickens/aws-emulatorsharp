using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Wire;

public static class WireModuleExtensions
{
    public static IServiceCollection AddWireModule(this IServiceCollection services)
    {
        return services;
    }
}
