using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public static class WireSurfaceExtensions
{
    private const string OperationHeaderName = "X-Amz-Target";

    public static Task RespondWithOperationName(this HttpContext context)
    {
        var operationName = context.Request.Headers[OperationHeaderName].ToString();
        return context.Response.WriteAsync(operationName);
    }
}
