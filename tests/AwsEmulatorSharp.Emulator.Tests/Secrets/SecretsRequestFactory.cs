using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.Secrets;

internal class SecretsRequestFactory
{
    private const string RequestContentType = "application/x-amz-json-1.1";
    private const string TargetHeaderName = "X-Amz-Target";
    private const string PutSecretValueTarget = "secretsmanager.PutSecretValue";
    private const string GetSecretValueTarget = "secretsmanager.GetSecretValue";
    private const string SecretIdPropertyName = "SecretId";
    private const string SecretStringPropertyName = "SecretString";

    internal DefaultHttpContext CreatePutSecretValueRequest(string secretName, string secretString)
    {
        var body = new JsonObject
        {
            [SecretIdPropertyName] = secretName,
            [SecretStringPropertyName] = secretString
        };

        var context = CreateRequest(PutSecretValueTarget, body);

        return context;
    }

    internal DefaultHttpContext CreateGetSecretValueRequest(string secretName)
    {
        var body = new JsonObject
        {
            [SecretIdPropertyName] = secretName
        };

        var context = CreateRequest(GetSecretValueTarget, body);

        return context;
    }

    private DefaultHttpContext CreateRequest(string target, JsonObject body)
    {
        var content = Encoding.UTF8.GetBytes(body.ToJsonString());
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/";
        context.Request.ContentType = RequestContentType;
        context.Request.Headers[TargetHeaderName] = target;
        context.Request.Body = new MemoryStream(content);
        context.Request.ContentLength = content.Length;
        context.Response.Body = new MemoryStream();

        return context;
    }
}
