using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

internal class CertificatesRequestFactory
{
    private const string RequestContentType = "application/x-amz-json-1.1";
    private const string TargetHeaderName = "X-Amz-Target";
    private const string TargetPrefix = "CertificateManager.";
    private const string RequestCertificateActionName = "RequestCertificate";
    private const string DescribeCertificateActionName = "DescribeCertificate";
    private const string GetCertificateActionName = "GetCertificate";
    private const string ListCertificatesActionName = "ListCertificates";
    private const string DeleteCertificateActionName = "DeleteCertificate";
    private const string DomainNamePropertyName = "DomainName";
    private const string CertificateArnPropertyName = "CertificateArn";

    internal DefaultHttpContext CreateRequestCertificateRequest(string domainName)
    {
        var body = new JsonObject
        {
            [DomainNamePropertyName] = domainName
        };

        var context = CreateRequest(TargetPrefix + RequestCertificateActionName, body);

        return context;
    }

    internal DefaultHttpContext CreateDescribeCertificateRequest(string certificateArn)
    {
        var context = CreateCertificateRequest(DescribeCertificateActionName, certificateArn);

        return context;
    }

    internal DefaultHttpContext CreateGetCertificateRequest(string certificateArn)
    {
        var context = CreateCertificateRequest(GetCertificateActionName, certificateArn);

        return context;
    }

    internal DefaultHttpContext CreateDeleteCertificateRequest(string certificateArn)
    {
        var context = CreateCertificateRequest(DeleteCertificateActionName, certificateArn);

        return context;
    }

    internal DefaultHttpContext CreateListCertificatesRequest()
    {
        var context = CreateRequest(TargetPrefix + ListCertificatesActionName, new JsonObject());

        return context;
    }

    internal DefaultHttpContext CreateRequestForAnotherService(string target)
    {
        var context = CreateRequest(target, new JsonObject());

        return context;
    }

    private DefaultHttpContext CreateCertificateRequest(string actionName, string certificateArn)
    {
        var body = new JsonObject
        {
            [CertificateArnPropertyName] = certificateArn
        };

        var context = CreateRequest(TargetPrefix + actionName, body);

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
