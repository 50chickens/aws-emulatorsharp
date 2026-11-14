using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Secrets;

public class SecretsResponseWriter
{
    private const string ResponseContentType = "application/x-amz-json-1.1";
    private const string SecretArnFormat = "arn:aws:secretsmanager:us-east-1:000000000000:secret:{0}";
    private const string ArnElementName = "ARN";
    private const string NameElementName = "Name";
    private const string VersionIdElementName = "VersionId";
    private const string VersionStagesElementName = "VersionStages";
    private const string SecretStringElementName = "SecretString";
    private const string CreatedDateElementName = "CreatedDate";
    private const string CurrentVersionStage = "AWSCURRENT";
    private const string ErrorTypeElementName = "__type";
    private const string ErrorMessageElementName = "message";

    public Task WritePutSecretValueAsync(HttpResponse response, SecretValue secretValue)
    {
        var document = BuildVersionDocument(secretValue);

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteGetSecretValueAsync(HttpResponse response, SecretValue secretValue)
    {
        var document = BuildVersionDocument(secretValue);
        document[SecretStringElementName] = secretValue.SecretString;
        document[CreatedDateElementName] = secretValue.CreatedDate.ToUnixTimeSeconds();

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

    private JsonObject BuildVersionDocument(SecretValue secretValue)
    {
        var arn = string.Format(CultureInfo.InvariantCulture, SecretArnFormat, secretValue.Name);
        var document = new JsonObject
        {
            [ArnElementName] = arn,
            [NameElementName] = secretValue.Name,
            [VersionIdElementName] = secretValue.VersionId,
            [VersionStagesElementName] = new JsonArray(CurrentVersionStage)
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
