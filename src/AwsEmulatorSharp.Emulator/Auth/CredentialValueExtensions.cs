namespace AwsEmulatorSharp.Emulator.Auth;

internal static class CredentialValueExtensions
{
    public static RequestCredential ToRequestCredential(this string credentialValue, SigningKind signingKind)
    {
        var scope = credentialValue.Split('/');

        return new RequestCredential(signingKind, scope[0], scope[2], scope[3]);
    }
}
