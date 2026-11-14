using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests;

[TimedTestFixture]
public class WireSurfaceTests
{
    private const string OperationHeaderName = "X-Amz-Target";
    private const string OperationName = "AmazonS3.ListBuckets";

    private DefaultHttpContext _context;
    private MemoryStream _body;

    [SetUp]
    public void SetUp()
    {
        _body = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Response.Body = _body;
        _context.Request.Headers[OperationHeaderName] = OperationName;
    }

    [Test]
    public async Task Test_That_We_Can_Read_A_Request_Header_And_Return_A_Response_Body()
    {
        await _context.RespondWithOperationName();
        _body.Position = 0;
        using var reader = new StreamReader(_body);
        var responseBody = reader.ReadToEnd();

        Assert.That(responseBody, Is.EqualTo(OperationName));
    }
}
