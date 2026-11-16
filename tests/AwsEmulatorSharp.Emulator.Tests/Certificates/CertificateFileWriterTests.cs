using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using AwsEmulatorSharp.Emulator.Certificates;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

[TimedTestFixture(1)]
public class CertificateFileWriterTests
{
    private const string DomainName = "installer.example.test";
    private const string FirstCertificateArn = "arn:aws:acm:us-east-1:000000000000:certificate/first";
    private const string SecondCertificateArn = "arn:aws:acm:us-east-1:000000000000:certificate/second";
    private const string CertificateFileName = "certificate.pem";
    private const string PfxFileName = "certificate.pfx";
    private const int WrittenFileCount = 4;

    private AcmCertificateAuthority _authority;
    private CertificateFileWriter _fileWriter;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _authority = new AcmCertificateAuthority(TimeProvider.System);
    }

    [SetUp]
    public void SetUp()
    {
        var configuration = new CertificatesConfigurationReader().Read();
        _fileWriter = new CertificateFileWriter(configuration);
    }

    [Test]
    public void Test_That_We_Write_Into_A_Folder_Named_For_The_Process_That_Runs_It()
    {
        var processFolderName = Path.GetFileName(_fileWriter.OutputFolder);
        var processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);

        Assert.That(processFolderName, Is.EqualTo(processId));
    }

    [Test]
    public void Test_That_We_Write_Two_Certificates_In_Sequence_Without_A_Write_Failing()
    {
        var firstCertificate = _authority.Issue(FirstCertificateArn, DomainName);
        _fileWriter.Write(firstCertificate);
        var firstWrittenFiles = Directory.GetFiles(_fileWriter.OutputFolder)
            .Select(filePath => Path.GetFileName(filePath))
            .ToList();

        var secondCertificate = _authority.Issue(SecondCertificateArn, DomainName);
        _fileWriter.Write(secondCertificate);
        var secondWrittenFiles = Directory.GetFiles(_fileWriter.OutputFolder)
            .Select(filePath => Path.GetFileName(filePath))
            .ToList();
        var writtenCertificatePem = File.ReadAllText(Path.Combine(_fileWriter.OutputFolder, CertificateFileName));
        using var writtenCertificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(_fileWriter.OutputFolder, PfxFileName), null);

        Assert.That(firstWrittenFiles, Has.Count.EqualTo(WrittenFileCount));
        Assert.That(secondWrittenFiles, Has.Count.EqualTo(WrittenFileCount));
        Assert.That(writtenCertificatePem, Is.EqualTo(secondCertificate.CertificatePem));
        Assert.That(writtenCertificate.HasPrivateKey, Is.True);
    }
}
