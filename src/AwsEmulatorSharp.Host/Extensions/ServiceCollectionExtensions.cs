using AwsEmulatorSharp.Emulator.Auth;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Host;
using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Images;
using AwsEmulatorSharp.Emulator.S3;
using AwsEmulatorSharp.Emulator.Secrets;
using AwsEmulatorSharp.Emulator.Wire;
using Coop.Library.Squawk.Extensions;

namespace AwsEmulatorSharp.Host.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddEmulatorModules(this IServiceCollection services)
    {
        services.AddSquawk();
        services.AddHostModule();
        services.AddContainersModule();
        services.AddWireModule();
        services.AddAuthModule();
        services.AddImagesModule();
        services.AddEc2Module();
        services.AddS3Module();
        services.AddIdentityModule();
        services.AddSecretsModule();

        return services;
    }
}
