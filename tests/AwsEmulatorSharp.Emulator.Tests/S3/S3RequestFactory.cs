using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.S3;

internal class S3RequestFactory
{
    internal DefaultHttpContext CreateRequest(string method, string path, string queryString)
    {
        var context = CreateRequest(method, path, Array.Empty<byte>(), queryString);

        return context;
    }

    internal DefaultHttpContext CreateRequest(string method, string path, byte[] content, string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        context.Request.Body = new MemoryStream(content);
        context.Request.ContentLength = content.Length;
        context.Response.Body = new MemoryStream();

        return context;
    }
}
