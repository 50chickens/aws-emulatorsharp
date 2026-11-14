using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Ec2;

public static class Ec2ModuleExtensions
{
    public static IServiceCollection AddEc2Module(this IServiceCollection services)
    {
        return services;
    }
}
