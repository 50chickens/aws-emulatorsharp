using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        var configurationReader = new IdentityConfigurationReader();
        var configuration = configurationReader.Read();

        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IdentityStore>();
        services.AddSingleton<IdentityResponseWriter>();
        services.AddSingleton<IQueryOperation, GetCallerIdentityOperation>();
        services.AddSingleton<IQueryOperation, AssumeRoleOperation>();

        return services;
    }
}
