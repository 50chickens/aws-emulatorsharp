using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

internal static class CertificatesResponseExtensions
{
    internal static JsonDocument ReadResponseDocument(this HttpContext context)
    {
        var responseBody = (MemoryStream)context.Response.Body;
        var document = JsonDocument.Parse(responseBody.ToArray());

        return document;
    }
}
