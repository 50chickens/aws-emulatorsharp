using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificatesRequestMiddleware
{
    private readonly CertificatesSurface _surface;
    private readonly RequestDelegate _next;

    public CertificatesRequestMiddleware(CertificatesSurface surface, RequestDelegate next)
    {
        _surface = surface;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_surface.CanHandle(context))
        {
            await _next(context);
            return;
        }

        await _surface.HandleAsync(context);
    }
}
