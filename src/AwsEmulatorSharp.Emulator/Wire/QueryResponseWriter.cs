using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public class QueryResponseWriter
{
    private const string ResponseContentType = "text/xml";
    private const string ErrorRootElementName = "Response";
    private const string ErrorsElementName = "Errors";
    private const string ErrorElementName = "Error";
    private const string CodeElementName = "Code";
    private const string MessageElementName = "Message";
    private const string ErrorRequestIdElementName = "RequestID";
    private const int RequestIdLength = 32;

    private readonly IWireConfiguration _configuration;

    public QueryResponseWriter(IWireConfiguration configuration)
    {
        _configuration = configuration;
    }

    public XNamespace XmlNamespace => _configuration.QueryNamespace;

    public string NextRequestId()
    {
        return RandomNumberGenerator.GetHexString(RequestIdLength, true);
    }

    public async Task WriteAsync(HttpResponse response, XDocument document)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = ResponseContentType;
        await response.WriteAsync(document.ToString(), Encoding.UTF8);
    }

    public Task WriteErrorAsync(HttpResponse response, string errorCode, string errorMessage)
    {
        response.StatusCode = StatusCodes.Status400BadRequest;
        response.ContentType = ResponseContentType;

        var document = BuildErrorDocument(errorCode, errorMessage);
        return response.WriteAsync(document.ToString(), Encoding.UTF8);
    }

    private XDocument BuildErrorDocument(string errorCode, string errorMessage)
    {
        var xmlNamespace = XmlNamespace;
        var requestId = NextRequestId();

        return new XDocument(
            new XElement(xmlNamespace + ErrorRootElementName,
                new XElement(xmlNamespace + ErrorsElementName,
                    new XElement(xmlNamespace + ErrorElementName,
                        new XElement(xmlNamespace + CodeElementName, errorCode),
                        new XElement(xmlNamespace + MessageElementName, errorMessage))),
                new XElement(xmlNamespace + ErrorRequestIdElementName, requestId)));
    }
}
