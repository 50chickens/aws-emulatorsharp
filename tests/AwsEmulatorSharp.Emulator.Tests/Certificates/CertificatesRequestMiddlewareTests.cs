using AwsEmulatorSharp.Emulator.Certificates;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Certificates;

[TimedTestFixture(1)]
public class CertificatesRequestMiddlewareTests
{
    private const string DomainName = "installer.example.test";
    private const string AnotherServiceTarget = "secretsmanager.GetSecretValue";
    private const string CertificateArnElementName = "CertificateArn";
    private const string CertificateArnPrefix = "arn:aws:acm:us-east-1:000000000000:certificate/";
    private const int PassedThroughStatusCode = 418;

    private AcmCertificateAuthority _authority;
    private CertificatesRequestMiddleware _middleware;
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
        var fileWriter = new CertificateFileWriter(configuration);
        var store = new CertificateStore(_authority, fileWriter);
        var surface = new CertificatesSurface(store, new CertificatesResponseWriter());

        _middleware = new CertificatesRequestMiddleware(surface, PassThroughAsync);
        _requestFactory = new CertificatesRequestFactory();
    }

    [Test]
    public async Task Test_That_We_Read_The_Certificate_Request_A_Client_Posts()
    {
        var context = _requestFactory.CreateRequestCertificateRequest(DomainName);

        await _middleware.InvokeAsync(context);

        using var document = context.ReadResponseDocument();
        var certificateArn = document.RootElement.GetProperty(CertificateArnElementName).ToString();

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(certificateArn, Does.Contain(CertificateArnPrefix));
    }

    [Test]
    public async Task Test_That_We_Pass_A_Request_For_Another_Service_To_The_Next_Handler()
    {
        var context = _requestFactory.CreateRequestForAnotherService(AnotherServiceTarget);

        await _middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(PassedThroughStatusCode));
    }

    private static Task PassThroughAsync(HttpContext context)
    {
        context.Response.StatusCode = PassedThroughStatusCode;

        return Task.CompletedTask;
    }
}
