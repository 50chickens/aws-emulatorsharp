using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Wire;

public static class WireModuleExtensions
{
    public static IServiceCollection AddWireModule(this IServiceCollection services)
    {
        services.AddSingleton<IWireConfiguration, WireConfiguration>();
        services.AddSingleton<QueryResponseWriter>();
        services.AddSingleton<WireSurface>();

        return services;
    }

    public static IEndpointRouteBuilder MapWireModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", (HttpContext context, WireSurface surface) => surface.HandleAsync(context));

        return endpoints;
    }
}
