using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.S3;

public static class S3ModuleExtensions
{
    public static IServiceCollection AddS3Module(this IServiceCollection services)
    {
        services.AddSingleton<ObjectStore>();
        services.AddSingleton<S3ResponseWriter>();
        services.AddSingleton<S3Surface>();

        return services;
    }

    public static IEndpointRouteBuilder MapS3Module(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map("/{bucketName}/{**objectKey}", (HttpContext context, S3Surface surface) => surface.HandleAsync(context));

        return endpoints;
    }
}
