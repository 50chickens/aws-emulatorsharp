using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Auth;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        return services;
    }
}
