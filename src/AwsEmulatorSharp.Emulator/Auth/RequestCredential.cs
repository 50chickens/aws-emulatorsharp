namespace AwsEmulatorSharp.Emulator.Auth;

public class RequestCredential
{
    public RequestCredential(SigningKind signingKind, string accessKey, string region, string service)
    {
        SigningKind = signingKind;
        AccessKey = accessKey;
        Region = region;
        Service = service;
    }

    public SigningKind SigningKind { get; }

    public string AccessKey { get; }

    public string Region { get; }

    public string Service { get; }
}
