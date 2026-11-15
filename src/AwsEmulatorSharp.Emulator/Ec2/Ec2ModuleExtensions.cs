using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Ec2;

public static class Ec2ModuleExtensions
{
    public static IServiceCollection AddEc2Module(this IServiceCollection services)
    {
        var configurationReader = new Ec2ConfigurationReader();
        var configuration = configurationReader.Read();

        services.AddSingleton(configuration);
        services.AddSingleton<IQueryOperation, DescribeRegionsOperation>();
        services.AddSingleton<IQueryOperation, DescribeAvailabilityZonesOperation>();
        services.AddSingleton<IQueryOperation, DescribeInstanceTypesOperation>();
        services.AddSingleton<IQueryOperation, DescribeKeyPairsOperation>();
        services.AddSingleton<IQueryOperation, DescribeSubnetsOperation>();
        services.AddSingleton<IQueryOperation, DescribeSecurityGroupsOperation>();
        services.AddSingleton<InstanceStore>();
        services.AddSingleton<IQueryOperation, RunInstancesOperation>();
        services.AddSingleton<IQueryOperation, DescribeInstancesOperation>();

        return services;
    }
}
