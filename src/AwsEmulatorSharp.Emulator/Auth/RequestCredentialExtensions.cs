using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Auth;

public static class RequestCredentialExtensions
{
    private const string PresignedCredentialQueryName = "X-Amz-Credential";
    private const string CredentialPrefix = "Credential=";

    public static RequestCredential ReadRequestCredential(this HttpRequest request)
    {
        var presignedCredential = request.Query[PresignedCredentialQueryName].ToString();
        if (presignedCredential.Length > 0)
        {
            return presignedCredential.ToRequestCredential(SigningKind.PresignedQuery);
        }

        var authorization = request.Headers.Authorization.ToString();
        var credentialStart = authorization.IndexOf(CredentialPrefix, StringComparison.Ordinal);
        if (credentialStart < 0)
        {
            return new RequestCredential(SigningKind.Unsigned, string.Empty, string.Empty, string.Empty);
        }

        var credentialValue = authorization.Substring(credentialStart + CredentialPrefix.Length);

        return credentialValue.ToRequestCredential(SigningKind.SignedHeader);
    }
}
