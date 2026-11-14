using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.S3;

internal static class RequestContentExtensions
{
    internal static async Task<byte[]> ReadContentAsync(this HttpRequest request)
    {
        var content = new MemoryStream();
        await request.Body.CopyToAsync(content);

        return content.ToArray();
    }
}
