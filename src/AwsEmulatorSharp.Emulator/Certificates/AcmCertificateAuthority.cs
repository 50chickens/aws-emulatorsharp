using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class AcmCertificateAuthority
{
    private const string AuthoritySubjectName = "CN=AwsEmulatorSharp emulator certificate authority";
    private const string SubjectNameFormat = "CN={0}";
    private const int KeySizeInBits = 2048;
    private const int AuthorityBackdateDays = 1;
    private const int AuthorityValidityYears = 10;
    private const int CertificateValidityDays = 365;
    private const int SerialNumberLength = 16;

    private readonly TimeProvider _timeProvider;
    private readonly X509Certificate2 _authorityCertificate;

    public AcmCertificateAuthority(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _authorityCertificate = CreateAuthorityCertificate();
    }

    public AcmCertificate Issue(string certificateArn, string domainName)
    {
        var issuedAt = _timeProvider.GetUtcNow();
        var notAfter = issuedAt.AddDays(CertificateValidityDays);
        var subjectName = string.Format(CultureInfo.InvariantCulture, SubjectNameFormat, domainName);
        var serialNumber = RandomNumberGenerator.GetBytes(SerialNumberLength);

        using var key = RSA.Create(KeySizeInBits);
        var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(BuildSubjectAlternativeName(domainName));

        using var certificate = request.Create(_authorityCertificate, issuedAt, notAfter, serialNumber);

        var certificatePem = certificate.ExportCertificatePem();
        var certificateChainPem = _authorityCertificate.ExportCertificatePem();
        var privateKeyPem = key.ExportPkcs8PrivateKeyPem();

        return new AcmCertificate(certificateArn, domainName, Convert.ToHexString(serialNumber), subjectName, _authorityCertificate.Subject, issuedAt, notAfter, certificatePem, certificateChainPem, privateKeyPem);
    }

    private X509Certificate2 CreateAuthorityCertificate()
    {
        var notBefore = _timeProvider.GetUtcNow().AddDays(-AuthorityBackdateDays);

        using var key = RSA.Create(KeySizeInBits);
        var request = new CertificateRequest(AuthoritySubjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(notBefore, notBefore.AddYears(AuthorityValidityYears));
    }

    private static X509Extension BuildSubjectAlternativeName(string domainName)
    {
        var alternativeNames = new SubjectAlternativeNameBuilder();
        alternativeNames.AddDnsName(domainName);

        return alternativeNames.Build();
    }
}
