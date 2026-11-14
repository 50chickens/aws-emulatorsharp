using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

public class DescribeKeyPairsOperation : IQueryOperation
{
    private const string KeyPairsActionName = "DescribeKeyPairs";
    private const string KeyNameParameterPrefix = "KeyName.";
    private const string ResponseRootElementName = "DescribeKeyPairsResponse";
    private const string RequestIdElementName = "requestId";
    private const string KeySetElementName = "keySet";
    private const string ItemElementName = "item";
    private const string KeyNameElementName = "keyName";
    private const string KeyFingerprintElementName = "keyFingerprint";
    private const string KeyTypeElementName = "keyType";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeKeyPairsOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => KeyPairsActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedKeyNames = form.ReadValues(KeyNameParameterPrefix);

        await _writer.WriteAsync(context.Response, BuildDocument(requestedKeyNames));
    }

    private XDocument BuildDocument(List<string> requestedKeyNames)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var keyPairs = _configuration.KeyPairs.SelectRequested(requestedKeyNames, keyPair => keyPair.Name);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + KeySetElementName,
                    keyPairs.Select(keyPair => new XElement(xmlNamespace + ItemElementName,
                        new XElement(xmlNamespace + KeyNameElementName, keyPair.Name),
                        new XElement(xmlNamespace + KeyFingerprintElementName, keyPair.Fingerprint),
                        new XElement(xmlNamespace + KeyTypeElementName, keyPair.KeyType))))));
    }
}
