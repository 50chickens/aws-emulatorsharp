namespace AwsEmulatorSharp.Emulator.Certificates;

public class AcmCertificate
{
    public AcmCertificate(string certificateArn, string domainName, string serialNumber, string subject, string issuer, DateTimeOffset issuedAt, DateTimeOffset notAfter, string certificatePem, string certificateChainPem, string privateKeyPem)
    {
        CertificateArn = certificateArn;
        DomainName = domainName;
        SerialNumber = serialNumber;
        Subject = subject;
        Issuer = issuer;
        IssuedAt = issuedAt;
        NotAfter = notAfter;
        CertificatePem = certificatePem;
        CertificateChainPem = certificateChainPem;
        PrivateKeyPem = privateKeyPem;
    }

    public string CertificateArn { get; }

    public string DomainName { get; }

    public string SerialNumber { get; }

    public string Subject { get; }

    public string Issuer { get; }

    public DateTimeOffset IssuedAt { get; }

    public DateTimeOffset NotAfter { get; }

    public string CertificatePem { get; }

    public string CertificateChainPem { get; }

    public string PrivateKeyPem { get; }
}
