using AwsEmulatorSharp.Emulator.Auth;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Auth;

[TimedTestFixture(1)]
public class RequestCredentialTests
{
    private const string SignedHeaderAuthorization = "AWS4-HMAC-SHA256 Credential=dummy-access-key/20240101/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-date, Signature=0f8b1a2c3d4e5f60";
    private const string PresignedQueryString = "?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=dummy-access-key%2F20240101%2Feu-west-1%2Fs3%2Faws4_request&X-Amz-Date=20240101T000000Z&X-Amz-SignedHeaders=host&X-Amz-Signature=0f8b1a2c3d4e5f60";

    private DefaultHttpContext _context;

    [SetUp]
    public void SetUp()
    {
        _context = new DefaultHttpContext();
    }

    [TestCase(SignedHeaderAuthorization, "", SigningKind.SignedHeader, "dummy-access-key", "us-east-1", "s3")]
    [TestCase("", PresignedQueryString, SigningKind.PresignedQuery, "dummy-access-key", "eu-west-1", "s3")]
    [TestCase("", "", SigningKind.Unsigned, "", "", "")]
    public void Test_That_We_Can_Read_The_Credential_From_A_Request(
        string authorization,
        string queryString,
        SigningKind expectedSigningKind,
        string expectedAccessKey,
        string expectedRegion,
        string expectedService)
    {
        _context.Request.Headers.Authorization = authorization;
        _context.Request.QueryString = new QueryString(queryString);

        var actual = _context.Request.ReadRequestCredential();

        Assert.That(actual.SigningKind, Is.EqualTo(expectedSigningKind));
        Assert.That(actual.AccessKey, Is.EqualTo(expectedAccessKey));
        Assert.That(actual.Region, Is.EqualTo(expectedRegion));
        Assert.That(actual.Service, Is.EqualTo(expectedService));
    }
}
