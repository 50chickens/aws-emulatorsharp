using System.Security.Cryptography.X509Certificates;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

internal static class CertificateChainExtensions
{
    internal static X509Certificate2Collection ReadCertificates(this string certificatePem)
    {
        var certificates = new X509Certificate2Collection();
        certificates.ImportFromPem(certificatePem);

        return certificates;
    }

    internal static bool ChainsTo(this X509Certificate2 certificate, X509Certificate2Collection certificateAuthority)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(certificateAuthority);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;

        return chain.Build(certificate);
    }
}
