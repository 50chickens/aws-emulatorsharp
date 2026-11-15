using System.Text;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.Identity;

internal class IdentityRequestFactory
{
    private const string RequestContentType = "application/x-www-form-urlencoded";
    private const string SessionTokenHeaderName = "X-Amz-Security-Token";
    private const string AuthorizationHeaderFormat = "AWS4-HMAC-SHA256 Credential={0}/20260101/us-east-1/sts/aws4_request, SignedHeaders=host;x-amz-date, Signature=0f8b1a2c3d4e5f60";
    private const string GetCallerIdentityForm = "Action=GetCallerIdentity&Version=2011-06-15";
    private const string AssumeRoleFormFormat = "Action=AssumeRole&Version=2011-06-15&RoleArn={0}&RoleSessionName={1}";

    internal DefaultHttpContext CreateGetCallerIdentityRequest(string accessKey, string sessionToken)
    {
        var context = CreateRequest(GetCallerIdentityForm, accessKey);
        context.Request.Headers[SessionTokenHeaderName] = sessionToken;

        return context;
    }

    internal DefaultHttpContext CreateAssumeRoleRequest(string accessKey, string roleArn, string roleSessionName)
    {
        var form = string.Format(AssumeRoleFormFormat, roleArn, roleSessionName);

        return CreateRequest(form, accessKey);
    }

    private DefaultHttpContext CreateRequest(string form, string accessKey)
    {
        var content = Encoding.UTF8.GetBytes(form);
        var authorization = string.Format(AuthorizationHeaderFormat, accessKey);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/";
        context.Request.ContentType = RequestContentType;
        context.Request.Headers.Authorization = authorization;
        context.Request.Body = new MemoryStream(content);
        context.Request.ContentLength = content.Length;
        context.Response.Body = new MemoryStream();

        return context;
    }
}
