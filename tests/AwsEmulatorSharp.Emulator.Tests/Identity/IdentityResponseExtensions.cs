using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.Identity;

internal static class IdentityResponseExtensions
{
    internal static XDocument ReadResponseDocument(this HttpContext context)
    {
        var responseBody = (MemoryStream)context.Response.Body;
        responseBody.Position = 0;
        var document = XDocument.Load(responseBody);

        return document;
    }
}
