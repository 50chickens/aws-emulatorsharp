using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Secrets;

public class SecretsSurface
{
    private const string TargetHeaderName = "X-Amz-Target";
    private const string TargetPrefix = "secretsmanager.";
    private const string PutSecretValueActionName = "PutSecretValue";
    private const string GetSecretValueActionName = "GetSecretValue";
    private const string SecretIdPropertyName = "SecretId";
    private const string SecretStringPropertyName = "SecretString";
    private const string ResourceNotFoundErrorType = "ResourceNotFoundException";
    private const string ResourceNotFoundMessage = "Secrets Manager can't find the specified secret.";
    private const string InvalidRequestErrorType = "InvalidRequestException";
    private const string InvalidRequestMessageFormat = "The operation '{0}' is not implemented by this emulator.";

    private readonly SecretStore _store;
    private readonly SecretsResponseWriter _writer;

    public SecretsSurface(SecretStore store, SecretsResponseWriter writer)
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
            case PutSecretValueActionName:
                await WritePutSecretValueAsync(context);
                return;
            case GetSecretValueActionName:
                await WriteGetSecretValueAsync(context);
                return;
            default:
                await WriteInvalidRequestAsync(context, action);
                return;
        }
    }

    private async Task WritePutSecretValueAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var secretName = request.RootElement.GetProperty(SecretIdPropertyName).ToString();
        var secretString = request.RootElement.GetProperty(SecretStringPropertyName).ToString();
        var secretValue = _store.PutSecretValue(secretName, secretString);

        await _writer.WritePutSecretValueAsync(context.Response, secretValue);
    }

    private async Task WriteGetSecretValueAsync(HttpContext context)
    {
        using var request = await JsonDocument.ParseAsync(context.Request.Body);
        var secretName = request.RootElement.GetProperty(SecretIdPropertyName).ToString();
        var secretValues = _store.FindSecretValue(secretName);

        if (secretValues.Count == 0)
        {
            await _writer.WriteErrorAsync(context.Response, ResourceNotFoundErrorType, ResourceNotFoundMessage);
            return;
        }

        var secretValue = secretValues.First();
        await _writer.WriteGetSecretValueAsync(context.Response, secretValue);
    }

    private Task WriteInvalidRequestAsync(HttpContext context, string action)
    {
        var message = string.Format(CultureInfo.InvariantCulture, InvalidRequestMessageFormat, action);

        return _writer.WriteErrorAsync(context.Response, InvalidRequestErrorType, message);
    }
}
