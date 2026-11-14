using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.S3;

internal static class S3ResponseExtensions
{
    internal static byte[] ReadResponseContent(this HttpContext context)
    {
        var responseBody = (MemoryStream)context.Response.Body;
        var content = responseBody.ToArray();

        return content;
    }

    internal static XDocument ReadResponseDocument(this HttpContext context)
    {
        var content = context.ReadResponseContent();
        var document = XDocument.Load(new MemoryStream(content));

        return document;
    }
}
