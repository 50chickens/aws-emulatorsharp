using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Identity;

public class IdentityResponseWriter
{
    private const string ResponseNamespace = "https://sts.amazonaws.com/doc/2011-06-15/";
    private const string ResponseContentType = "text/xml";
    private const string GetCallerIdentityResponseElementName = "GetCallerIdentityResponse";
    private const string GetCallerIdentityResultElementName = "GetCallerIdentityResult";
    private const string AccountElementName = "Account";
    private const string ArnElementName = "Arn";
    private const string AssumeRoleResponseElementName = "AssumeRoleResponse";
    private const string AssumeRoleResultElementName = "AssumeRoleResult";
    private const string CredentialsElementName = "Credentials";
    private const string AccessKeyIdElementName = "AccessKeyId";
    private const string SecretAccessKeyElementName = "SecretAccessKey";
    private const string SessionTokenElementName = "SessionToken";
    private const string ExpirationElementName = "Expiration";
    private const string ExpirationFormat = "yyyy-MM-ddTHH:mm:ss'Z'";
    private const string ResponseMetadataElementName = "ResponseMetadata";
    private const string RequestIdElementName = "RequestId";
    private const string ErrorResponseElementName = "ErrorResponse";
    private const string ErrorElementName = "Error";
    private const string ErrorTypeElementName = "Type";
    private const string ErrorCodeElementName = "Code";
    private const string ErrorMessageElementName = "Message";
    private const string SenderErrorType = "Sender";
    private const int RequestIdLength = 32;

    public Task WriteGetCallerIdentityAsync(HttpResponse response, IdentityCaller caller)
    {
        return WriteDocumentAsync(response, StatusCodes.Status200OK, BuildGetCallerIdentityDocument(caller));
    }

    public Task WriteAssumeRoleAsync(HttpResponse response, AssumedRoleSession session)
    {
        return WriteDocumentAsync(response, StatusCodes.Status200OK, BuildAssumeRoleDocument(session));
    }

    public Task WriteErrorAsync(HttpResponse response, string errorCode, string errorMessage)
    {
        return WriteDocumentAsync(response, StatusCodes.Status400BadRequest, BuildErrorDocument(errorCode, errorMessage));
    }

    private XDocument BuildGetCallerIdentityDocument(IdentityCaller caller)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);

        return new XDocument(
            new XElement(xmlNamespace + GetCallerIdentityResponseElementName,
                new XElement(xmlNamespace + GetCallerIdentityResultElementName,
                    new XElement(xmlNamespace + ArnElementName, caller.Arn),
                    new XElement(xmlNamespace + AccountElementName, caller.Account)),
                BuildResponseMetadataElement(xmlNamespace)));
    }

    private XDocument BuildAssumeRoleDocument(AssumedRoleSession session)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var expiration = session.Expiration.UtcDateTime.ToString(ExpirationFormat, CultureInfo.InvariantCulture);

        return new XDocument(
            new XElement(xmlNamespace + AssumeRoleResponseElementName,
                new XElement(xmlNamespace + AssumeRoleResultElementName,
                    new XElement(xmlNamespace + CredentialsElementName,
                        new XElement(xmlNamespace + AccessKeyIdElementName, session.AccessKeyId),
                        new XElement(xmlNamespace + SecretAccessKeyElementName, session.SecretAccessKey),
                        new XElement(xmlNamespace + SessionTokenElementName, session.SessionToken),
                        new XElement(xmlNamespace + ExpirationElementName, expiration))),
                BuildResponseMetadataElement(xmlNamespace)));
    }

    private XDocument BuildErrorDocument(string errorCode, string errorMessage)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var requestId = RandomNumberGenerator.GetHexString(RequestIdLength, true);

        return new XDocument(
            new XElement(xmlNamespace + ErrorResponseElementName,
                new XElement(xmlNamespace + ErrorElementName,
                    new XElement(xmlNamespace + ErrorTypeElementName, SenderErrorType),
                    new XElement(xmlNamespace + ErrorCodeElementName, errorCode),
                    new XElement(xmlNamespace + ErrorMessageElementName, errorMessage)),
                new XElement(xmlNamespace + RequestIdElementName, requestId)));
    }

    private XElement BuildResponseMetadataElement(XNamespace xmlNamespace)
    {
        return new XElement(xmlNamespace + ResponseMetadataElementName,
            new XElement(xmlNamespace + RequestIdElementName, RandomNumberGenerator.GetHexString(RequestIdLength, true)));
    }

    private Task WriteDocumentAsync(HttpResponse response, int statusCode, XDocument document)
    {
        response.StatusCode = statusCode;
        response.ContentType = ResponseContentType;

        return response.WriteAsync(document.ToString(), Encoding.UTF8);
    }
}
