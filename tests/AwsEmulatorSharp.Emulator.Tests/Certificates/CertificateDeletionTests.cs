using AwsEmulatorSharp.Emulator.Certificates;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

[TimedTestFixture(1)]
public class CertificateDeletionTests
{
    private const string DomainName = "installer.example.test";
    private const string CertificateArnElementName = "CertificateArn";
    private const string CertificateSummaryListElementName = "CertificateSummaryList";

    private CertificatesSurface _surface;
    private CertificatesRequestFactory _requestFactory;
    private string _certificateArn;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var authority = new AcmCertificateAuthority(TimeProvider.System);
        var configuration = new CertificatesConfigurationReader().Read();
        var fileWriter = new CertificateFileWriter(configuration);
        var store = new CertificateStore(authority, fileWriter);
        _surface = new CertificatesSurface(store, new CertificatesResponseWriter());
        var requestCertificateContext = new CertificatesRequestFactory().CreateRequestCertificateRequest(DomainName);
        await _surface.HandleAsync(requestCertificateContext);
        using var requestCertificateDocument = requestCertificateContext.ReadResponseDocument();
        _certificateArn = requestCertificateDocument.RootElement.GetProperty(CertificateArnElementName).ToString();
    }

    [SetUp]
    public void SetUp()
    {
        _requestFactory = new CertificatesRequestFactory();
    }

    [Test]
    public async Task Test_That_A_Deleted_Request_Is_No_Longer_Listed()
    {
        var listBeforeDeleteContext = _requestFactory.CreateListCertificatesRequest();
        await _surface.HandleAsync(listBeforeDeleteContext);
        using var listBeforeDeleteDocument = listBeforeDeleteContext.ReadResponseDocument();
        var listedBeforeDelete = listBeforeDeleteDocument.RootElement
            .GetProperty(CertificateSummaryListElementName)
            .EnumerateArray()
            .Select(summary => summary.GetProperty(CertificateArnElementName).ToString())
            .ToList();

        var deleteCertificateContext = _requestFactory.CreateDeleteCertificateRequest(_certificateArn);
        await _surface.HandleAsync(deleteCertificateContext);

        var listAfterDeleteContext = _requestFactory.CreateListCertificatesRequest();
        await _surface.HandleAsync(listAfterDeleteContext);
        using var listAfterDeleteDocument = listAfterDeleteContext.ReadResponseDocument();
        var listedAfterDelete = listAfterDeleteDocument.RootElement
            .GetProperty(CertificateSummaryListElementName)
            .EnumerateArray()
            .Select(summary => summary.GetProperty(CertificateArnElementName).ToString())
            .ToList();

        Assert.That(listedBeforeDelete, Does.Contain(_certificateArn));
        Assert.That(deleteCertificateContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(listedAfterDelete, Has.Count.EqualTo(0));
    }
}
