using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Images;

public static class ImagesModuleExtensions
{
    public static IServiceCollection AddImagesModule(this IServiceCollection services)
    {
        services.AddSingleton<IQueryOperation, DescribeImagesOperation>();

        return services;
    }
}
