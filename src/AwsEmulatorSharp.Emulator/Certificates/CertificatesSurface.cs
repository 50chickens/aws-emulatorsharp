using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificatesSurface
{
    private const string TargetHeaderName = "X-Amz-Target";
    private const string TargetPrefix = "CertificateManager.";
    private const string RequestCertificateActionName = "RequestCertificate";
    private const string DescribeCertificateActionName = "DescribeCertificate";
    private const string GetCertificateActionName = "GetCertificate";
    private const string ListCertificatesActionName = "ListCertificates";
    private const string DeleteCertificateActionName = "DeleteCertificate";
    private const string DomainNamePropertyName = "DomainName";
    private const string CertificateArnPropertyName = "CertificateArn";
    private const string ResourceNotFoundErrorType = "ResourceNotFoundException";
    private const string ResourceNotFoundMessageFormat = "Certificate with arn {0} not found.";
    private const string InvalidRequestErrorType = "InvalidRequestException";
    private const string InvalidRequestMessageFormat = "The operation '{0}' is not implemented by this emulator.";

    private readonly CertificateStore _store;
    private readonly CertificatesResponseWriter _writer;

    public CertificatesSurface(CertificateStore store, CertificatesResponseWriter writer)
    {
        _store = store;
        _writer = writer;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var target = context.Request.Headers[TargetHeaderName].ToString();
        var action = target.StartsWith(TargetPrefix, StringComparison.Ordinal) ? target[TargetPrefix.Length..] : target;

        switch (action)
        {
            case RequestCertificateActionName:
                await WriteRequestCertificateAsync(context);
                return;
            case DescribeCertificateActionName:
                await WriteDescribeCertificateAsync(context);
                return;
            case GetCertificateActionName:
                await WriteGetCertificateAsync(context);
                return;
            case ListCertificatesActionName:
                await WriteListCertificatesAsync(context);
                return;
            case DeleteCertificateActionName:
                await WriteDeleteCertificateAsync(context);
                return;
            default:
                await WriteInvalidRequestAsync(context, action);
                return;
        }
    }

    private async Task WriteRequestCertificateAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var domainName = request.RootElement.GetProperty(DomainNamePropertyName).ToString();
        var certificate = _store.RequestCertificate(domainName);

        await _writer.WriteRequestCertificateAsync(context.Response, certificate);
    }

    private async Task WriteDescribeCertificateAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var certificateArn = request.RootElement.GetProperty(CertificateArnPropertyName).ToString();
        var certificates = _store.FindCertificate(certificateArn);

        if (certificates.Count == 0)
        {
            await WriteNotFoundAsync(context, certificateArn);
            return;
        }

        await _writer.WriteDescribeCertificateAsync(context.Response, certificates[0]);
    }

    private async Task WriteGetCertificateAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var certificateArn = request.RootElement.GetProperty(CertificateArnPropertyName).ToString();
        var certificates = _store.FindCertificate(certificateArn);

        if (certificates.Count == 0)
        {
            await WriteNotFoundAsync(context, certificateArn);
            return;
        }

        await _writer.WriteGetCertificateAsync(context.Response, certificates[0]);
    }

    private Task WriteListCertificatesAsync(HttpContext context)
    {
        var certificates = _store.FindCertificates();

        return _writer.WriteListCertificatesAsync(context.Response, certificates);
    }

    private async Task WriteDeleteCertificateAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var certificateArn = request.RootElement.GetProperty(CertificateArnPropertyName).ToString();
        var certificates = _store.FindCertificate(certificateArn);

        if (certificates.Count == 0)
        {
            await WriteNotFoundAsync(context, certificateArn);
            return;
        }

        _store.RemoveCertificate(certificateArn);
        await _writer.WriteDeleteCertificateAsync(context.Response);
    }

    private Task WriteNotFoundAsync(HttpContext context, string certificateArn)
    {
        var message = string.Format(CultureInfo.InvariantCulture, ResourceNotFoundMessageFormat, certificateArn);

        return _writer.WriteErrorAsync(context.Response, ResourceNotFoundErrorType, message);
    }

    private Task WriteInvalidRequestAsync(HttpContext context, string action)
    {
        var message = string.Format(CultureInfo.InvariantCulture, InvalidRequestMessageFormat, action);

        return _writer.WriteErrorAsync(context.Response, InvalidRequestErrorType, message);
    }
}
