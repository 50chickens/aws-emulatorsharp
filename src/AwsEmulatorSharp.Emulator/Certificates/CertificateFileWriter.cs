using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace AwsEmulatorSharp.Emulator.Certificates;

public class CertificateFileWriter
{
    private const string CertificateFileName = "certificate.pem";
    private const string ChainFileName = "chain.pem";
    private const string PrivateKeyFileName = "private-key.pem";
    private const string PfxFileName = "certificate.pfx";

    public CertificateFileWriter(CertificatesConfiguration configuration)
    {
        var processFolderName = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        OutputFolder = Path.Combine(AppContext.BaseDirectory, configuration.OutputFolder, processFolderName);
    }

    public string OutputFolder { get; }

    public void Write(AcmCertificate certificate)
    {
        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, CertificateFileName), certificate.CertificatePem);
        File.WriteAllText(Path.Combine(OutputFolder, ChainFileName), certificate.CertificateChainPem);
        File.WriteAllText(Path.Combine(OutputFolder, PrivateKeyFileName), certificate.PrivateKeyPem);

        using var certificateWithKey = X509Certificate2.CreateFromPem(certificate.CertificatePem, certificate.PrivateKeyPem);
        File.WriteAllBytes(Path.Combine(OutputFolder, PfxFileName), certificateWithKey.Export(X509ContentType.Pfx));
    }
}
