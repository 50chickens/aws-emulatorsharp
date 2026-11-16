using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwsEmulatorSharp.Emulator.Certificates;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

[TimedTestFixture(1)]
public class CertificatesSurfaceTests
{
    private const string DomainName = "installer.example.test";
    private const string AbsentCertificateArn = "arn:aws:acm:us-east-1:000000000000:certificate/absent";
    private const string CertificateArnPrefix = "arn:aws:acm:us-east-1:000000000000:certificate/";
    private const string CertificateArnElementName = "CertificateArn";
    private const string CertificateElementName = "Certificate";
    private const string CertificateChainElementName = "CertificateChain";
    private const string DomainNameElementName = "DomainName";
    private const string StatusElementName = "Status";
    private const string ErrorTypeElementName = "__type";
    private const string IssuedStatus = "ISSUED";
    private const string ResourceNotFoundErrorType = "ResourceNotFoundException";
    private const string ResponseContentType = "application/x-amz-json-1.1";
    private const string CertificateFileName = "certificate.pem";
    private const string ChainFileName = "chain.pem";
    private const string PrivateKeyFileName = "private-key.pem";
    private const string PfxFileName = "certificate.pfx";

    private AcmCertificate _certificate;
    private CertificateFileWriter _fileWriter;
    private CertificateStore _store;
    private CertificatesSurface _surface;
    private CertificatesRequestFactory _requestFactory;
    private HttpContext _requestCertificateContext;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var authority = new AcmCertificateAuthority(TimeProvider.System);
        var configuration = new CertificatesConfigurationReader().Read();
        _fileWriter = new CertificateFileWriter(configuration);
        _store = new CertificateStore(authority, _fileWriter);
        _surface = new CertificatesSurface(_store, new CertificatesResponseWriter());
        _requestCertificateContext = new CertificatesRequestFactory().CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(_requestCertificateContext);
        using var requestCertificateDocument = _requestCertificateContext.ReadResponseDocument();
        var certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();
        _certificate = _store.FindCertificate(certificateArn)[0];
    }

    [SetUp]
    public void SetUp()
    {
        _requestFactory = new CertificatesRequestFactory();
    }

    [Test]
    public void Test_That_We_Write_The_Files_An_Instance_Imports_When_We_Issue_A_Certificate()
    {
        var writtenFiles = Directory.GetFiles(_fileWriter.OutputFolder)
            .Select(filePath => Path.GetFileName(filePath))
            .ToList();

        Assert.That(writtenFiles, Does.Contain(CertificateFileName));
        Assert.That(writtenFiles, Does.Contain(ChainFileName));
        Assert.That(writtenFiles, Does.Contain(PrivateKeyFileName));
        Assert.That(writtenFiles, Does.Contain(PfxFileName));
    }

    [Test]
    public void Test_That_The_Written_Pfx_Carries_A_Certificate_That_Chains_To_The_Emulator_Certificate_Authority()
    {
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(_fileWriter.OutputFolder, PfxFileName), null);
        var certificateAuthority = File.ReadAllText(Path.Combine(_fileWriter.OutputFolder, ChainFileName)).ReadCertificates();

        Assert.That(certificate.ChainsTo(certificateAuthority), Is.True);
        Assert.That(certificate.HasPrivateKey, Is.True);
    }

    [Test]
    public async Task Test_That_We_Issue_A_Certificate_That_Chains_To_The_Emulator_Certificate_Authority()
    {
        var getCertificateContext = _requestFactory.CreateGetCertificateRequest(_certificate.CertificateArn);
        await _surface.HandleAsync(getCertificateContext);
        using var getCertificateDocument = getCertificateContext.ReadResponseDocument();
        var certificatePem = getCertificateDocument.RootElement.GetProperty(CertificateElementName).ToString();
        var certificateChainPem = getCertificateDocument.RootElement.GetProperty(CertificateChainElementName).ToString();

        var certificate = certificatePem.ReadCertificates()[0];
        var certificateAuthority = certificateChainPem.ReadCertificates();

        Assert.That(_requestCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(_certificate.CertificateArn, Does.Contain(CertificateArnPrefix));
        Assert.That(getCertificateContext.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(certificate.ChainsTo(certificateAuthority), Is.True);
        Assert.That(certificate.Issuer, Is.EqualTo(certificateAuthority[0].Subject));
        Assert.That(certificate.GetNameInfo(X509NameType.DnsName, false), Is.EqualTo(DomainName));
    }

    [Test]
    public void Test_That_We_Store_The_Certificate_Chain_And_Private_Key_Against_The_Request()
    {
        var storedCertificate = _store.FindCertificate(_certificate.CertificateArn)[0];
        var certificate = storedCertificate.CertificatePem.ReadCertificates()[0];
        var certificateAuthority = storedCertificate.CertificateChainPem.ReadCertificates();
        using var storedKey = RSA.Create();
        using var certificateKey = certificate.GetRSAPublicKey()!;
        storedKey.ImportFromPem(storedCertificate.PrivateKeyPem);

        Assert.That(storedCertificate.DomainName, Is.EqualTo(DomainName));
        Assert.That(certificate.ChainsTo(certificateAuthority), Is.True);
        Assert.That(storedKey.ExportSubjectPublicKeyInfo(), Is.EqualTo(certificateKey.ExportSubjectPublicKeyInfo()));
    }

    [Test]
    public async Task Test_That_We_Describe_The_Certificate_We_Requested()
    {
        var describeCertificateContext = _requestFactory.CreateDescribeCertificateRequest(_certificate.CertificateArn);
        await _surface.HandleAsync(describeCertificateContext);
        using var describeCertificateDocument = describeCertificateContext.ReadResponseDocument();
        var certificate = describeCertificateDocument.RootElement.GetProperty(CertificateElementName);
        var describedCertificateArn = certificate.GetProperty(CertificateArnElementName).ToString();
        var describedDomainName = certificate.GetProperty(DomainNameElementName).ToString();
        var describedStatus = certificate.GetProperty(StatusElementName).ToString();

        Assert.That(describeCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(describedCertificateArn, Is.EqualTo(_certificate.CertificateArn));
        Assert.That(describedDomainName, Is.EqualTo(DomainName));
        Assert.That(describedStatus, Is.EqualTo(IssuedStatus));
    }

    [Test]
    public async Task Test_That_We_Answer_ResourceNotFound_For_A_Certificate_That_Was_Not_Requested()
    {
        var describeCertificateContext = _requestFactory.CreateDescribeCertificateRequest(AbsentCertificateArn);
        await _surface.HandleAsync(describeCertificateContext);
        using var describeCertificateDocument = describeCertificateContext.ReadResponseDocument();
        var errorType = describeCertificateDocument.RootElement.GetProperty(ErrorTypeElementName).ToString();

        Assert.That(describeCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorType, Is.EqualTo(ResourceNotFoundErrorType));
    }
}
