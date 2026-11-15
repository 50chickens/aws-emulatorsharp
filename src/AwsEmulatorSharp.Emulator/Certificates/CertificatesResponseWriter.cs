using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificatesResponseWriter
{
    private const string ResponseContentType = "application/x-amz-json-1.1";
    private const string CertificateArnElementName = "CertificateArn";
    private const string CertificateElementName = "Certificate";
    private const string CertificateChainElementName = "CertificateChain";
    private const string CertificateSummaryListElementName = "CertificateSummaryList";
    private const string DomainNameElementName = "DomainName";
    private const string SubjectElementName = "Subject";
    private const string IssuerElementName = "Issuer";
    private const string StatusElementName = "Status";
    private const string SerialNumberElementName = "SerialNumber";
    private const string IssuedAtElementName = "IssuedAt";
    private const string CreatedAtElementName = "CreatedAt";
    private const string NotAfterElementName = "NotAfter";
    private const string TypeElementName = "Type";
    private const string IssuedStatus = "ISSUED";
    private const string IssuedType = "AMAZON_ISSUED";
    private const string ErrorTypeElementName = "__type";
    private const string ErrorMessageElementName = "message";

    public Task WriteRequestCertificateAsync(HttpResponse response, AcmCertificate certificate)
    {
        var document = new JsonObject
        {
            [CertificateArnElementName] = certificate.CertificateArn
        };

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteDescribeCertificateAsync(HttpResponse response, AcmCertificate certificate)
    {
        var document = new JsonObject
        {
            [CertificateElementName] = BuildCertificateDocument(certificate)
        };

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteGetCertificateAsync(HttpResponse response, AcmCertificate certificate)
    {
        var document = new JsonObject
        {
            [CertificateElementName] = certificate.CertificatePem,
            [CertificateChainElementName] = certificate.CertificateChainPem
        };

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteListCertificatesAsync(HttpResponse response, IReadOnlyList<AcmCertificate> certificates)
    {
        var summaries = certificates.Select(BuildSummaryDocument).ToList();
        var document = new JsonObject
        {
            [CertificateSummaryListElementName] = new JsonArray(summaries.ToArray())
        };

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteDeleteCertificateAsync(HttpResponse response)
    {
        var document = new JsonObject();

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteErrorAsync(HttpResponse response, string errorType, string errorMessage)
    {
        var document = new JsonObject
        {
            [ErrorTypeElementName] = errorType,
            [ErrorMessageElementName] = errorMessage
        };

        return WriteDocumentAsync(response, StatusCodes.Status400BadRequest, document);
    }

    private JsonObject BuildCertificateDocument(AcmCertificate certificate)
    {
        var document = new JsonObject
        {
            [CertificateArnElementName] = certificate.CertificateArn,
            [DomainNameElementName] = certificate.DomainName,
            [SubjectElementName] = certificate.Subject,
            [IssuerElementName] = certificate.Issuer,
            [StatusElementName] = IssuedStatus,
            [SerialNumberElementName] = certificate.SerialNumber,
            [TypeElementName] = IssuedType,
            [IssuedAtElementName] = certificate.IssuedAt.ToUnixTimeSeconds(),
            [NotAfterElementName] = certificate.NotAfter.ToUnixTimeSeconds()
        };

        return document;
    }

    private JsonObject BuildSummaryDocument(AcmCertificate certificate)
    {
        var document = new JsonObject
        {
            [CertificateArnElementName] = certificate.CertificateArn,
            [DomainNameElementName] = certificate.DomainName,
            [StatusElementName] = IssuedStatus,
            [TypeElementName] = IssuedType,
            [CreatedAtElementName] = certificate.IssuedAt.ToUnixTimeSeconds(),
            [NotAfterElementName] = certificate.NotAfter.ToUnixTimeSeconds()
        };

        return document;
    }

    private Task WriteDocumentAsync(HttpResponse response, int statusCode, JsonObject document)
    {
        response.StatusCode = statusCode;
        response.ContentType = ResponseContentType;

        return response.WriteAsync(document.ToJsonString(), Encoding.UTF8);
    }
}
