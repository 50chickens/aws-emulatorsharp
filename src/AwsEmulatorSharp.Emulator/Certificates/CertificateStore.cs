using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificateStore
{
    private const string CertificateArnFormat = "arn:aws:acm:us-east-1:000000000000:certificate/{0}";
    private const int CertificateIdLength = 32;

    private readonly AcmCertificateAuthority _authority;
    private readonly ConcurrentDictionary<string, AcmCertificate> _certificates = new(StringComparer.Ordinal);

    public CertificateStore(AcmCertificateAuthority authority)
    {
        _authority = authority;
    }

    public AcmCertificate RequestCertificate(string domainName)
    {
        var certificateId = RandomNumberGenerator.GetHexString(CertificateIdLength, true);
        var certificateArn = string.Format(CultureInfo.InvariantCulture, CertificateArnFormat, certificateId);
        var certificate = _authority.Issue(certificateArn, domainName);
        _certificates[certificateArn] = certificate;

        return certificate;
    }

    public IReadOnlyList<AcmCertificate> FindCertificate(string certificateArn)
    {
        if (!_certificates.TryGetValue(certificateArn, out var certificate))
        {
            var absentCertificates = new List<AcmCertificate>();

            return absentCertificates;
        }

        var foundCertificates = new List<AcmCertificate> { certificate };

        return foundCertificates;
    }

    public IReadOnlyList<AcmCertificate> FindCertificates()
    {
        var certificates = _certificates.Values.ToList();

        return certificates;
    }

    public void RemoveCertificate(string certificateArn)
    {
        _certificates.TryRemove(certificateArn, out _);
    }
}
