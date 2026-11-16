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
    private const string CertificateSummaryListElementName = "CertificateSummaryList";
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

    private AcmCertificateAuthority _authority;
    private CertificateFileWriter _fileWriter;
    private CertificateStore _store;
    private CertificatesSurface _surface;
    private CertificatesRequestFactory _requestFactory;

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
        _store = new CertificateStore(_authority, _fileWriter);
        _surface = new CertificatesSurface(_store, new CertificatesResponseWriter());
        _requestFactory = new CertificatesRequestFactory();
    }

    [Test]
    public async Task Test_That_We_Write_The_Files_An_Instance_Imports_When_We_Issue_A_Certificate()
    {
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);

        var writtenFiles = Directory.GetFiles(_fileWriter.OutputFolder)
            .Select(filePath => Path.GetFileName(filePath))
            .ToList();

        Assert.That(writtenFiles, Does.Contain(CertificateFileName));
        Assert.That(writtenFiles, Does.Contain(ChainFileName));
        Assert.That(writtenFiles, Does.Contain(PrivateKeyFileName));
        Assert.That(writtenFiles, Does.Contain(PfxFileName));
    }

    [Test]
    public async Task Test_That_The_Written_Pfx_Carries_A_Certificate_That_Chains_To_The_Emulator_Certificate_Authority()
    {
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);

        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(_fileWriter.OutputFolder, PfxFileName), null);
        var certificateAuthority = File.ReadAllText(Path.Combine(_fileWriter.OutputFolder, ChainFileName)).ReadCertificates();

        Assert.That(certificate.ChainsTo(certificateAuthority), Is.True);
        Assert.That(certificate.HasPrivateKey, Is.True);
    }

    [Test]
    public async Task Test_That_We_Issue_A_Certificate_That_Chains_To_The_Emulator_Certificate_Authority()
    {
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);
        using var requestCertificateDocument = requestCertificateContext.ReadResponseDocument();
        var certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();

        var getCertificateContext = _requestFactory.CreateGetCertificateRequest(certificateArn);
        await _surface.HandleAsync(getCertificateContext);
        using var getCertificateDocument = getCertificateContext.ReadResponseDocument();
        var certificatePem = getCertificateDocument.RootElement.GetProperty(CertificateElementName).ToString();
        var certificateChainPem = getCertificateDocument.RootElement.GetProperty(CertificateChainElementName).ToString();

        var certificate = certificatePem.ReadCertificates()[0];
        var certificateAuthority = certificateChainPem.ReadCertificates();

        Assert.That(requestCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(certificateArn, Does.Contain(CertificateArnPrefix));
        Assert.That(getCertificateContext.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(certificate.ChainsTo(certificateAuthority), Is.True);
        Assert.That(certificate.Issuer, Is.EqualTo(certificateAuthority[0].Subject));
        Assert.That(certificate.GetNameInfo(X509NameType.DnsName, false), Is.EqualTo(DomainName));
    }

    [Test]
    public async Task Test_That_We_Store_The_Certificate_Chain_And_Private_Key_Against_The_Request()
    {
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);
        using var requestCertificateDocument = requestCertificateContext.ReadResponseDocument();
        var certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();

        var storedCertificate = _store.FindCertificate(certificateArn)[0];
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
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);
        using var requestCertificateDocument = requestCertificateContext.ReadResponseDocument();
        var certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();

        var describeCertificateContext = _requestFactory.CreateDescribeCertificateRequest(certificateArn);
        await _surface.HandleAsync(describeCertificateContext);
        using var describeCertificateDocument = describeCertificateContext.ReadResponseDocument();
        var certificate = describeCertificateDocument.RootElement.GetProperty(CertificateElementName);
        var describedCertificateArn = certificate.GetProperty(CertificateArnElementName).ToString();
        var describedDomainName = certificate.GetProperty(DomainNameElementName).ToString();
        var describedStatus = certificate.GetProperty(StatusElementName).ToString();

        Assert.That(describeCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(describedCertificateArn, Is.EqualTo(certificateArn));
        Assert.That(describedDomainName, Is.EqualTo(DomainName));
        Assert.That(describedStatus, Is.EqualTo(IssuedStatus));
    }

    [Test]
    public async Task Test_That_A_Deleted_Request_Is_No_Longer_Listed()
    {
        var requestCertificateContext = _requestFactory.CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);
        using var requestCertificateDocument = requestCertificateContext.ReadResponseDocument();
        var certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();

        var listBeforeDeleteContext = _requestFactory.CreateListCertificatesRequest();
        await _surface.HandleAsync(listBeforeDeleteContext);
        using var listBeforeDeleteDocument = listBeforeDeleteContext.ReadResponseDocument();
        var listedBeforeDelete = listBeforeDeleteDocument.RootElement
            .GetProperty(CertificateSummaryListElementName)
            .EnumerateArray()
            .Select(summary => summary.GetProperty(CertificateArnElementName).ToString())
            .ToList();

        var deleteCertificateContext = _requestFactory.CreateDeleteCertificateRequest(certificateArn);
        await _surface.HandleAsync(deleteCertificateContext);

        var listAfterDeleteContext = _requestFactory.CreateListCertificatesRequest();
        await _surface.HandleAsync(listAfterDeleteContext);
        using var listAfterDeleteDocument = listAfterDeleteContext.ReadResponseDocument();
        var listedAfterDelete = listAfterDeleteDocument.RootElement
            .GetProperty(CertificateSummaryListElementName)
            .EnumerateArray()
            .Select(summary => summary.GetProperty(CertificateArnElementName).ToString())
            .ToList();

        Assert.That(listedBeforeDelete, Does.Contain(certificateArn));
        Assert.That(deleteCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(listedAfterDelete, Has.Count.EqualTo(0));
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
