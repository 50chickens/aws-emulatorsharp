using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        return services;
    }
}
