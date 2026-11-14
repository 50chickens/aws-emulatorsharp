using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Wire;

[TimedTestFixture]
public class WireSurfaceTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string ResponseContentType = "text/xml";
    private const string DescribeRegionsRequest = "Action=DescribeRegions";
    private const string RegionName = "us-east-1";
    private const string RegionEndpoint = "ec2.us-east-1.amazonaws.com";
    private const int RequestIdLength = 32;

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;

    [SetUp]
    public void SetUp()
    {
        var configuration = new WireConfiguration();
        var writer = new QueryResponseWriter(configuration);
        var describeRegionsOperation = new DescribeRegionsOperation(configuration, writer);

        _surface = new WireSurface(describeRegionsOperation, writer);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [TestCase(DescribeRegionsRequest)]
    [TestCase("Action=DescribeRegions&RegionName.1=us-east-1")]
    public async Task Test_That_We_Can_Describe_Regions(string formBody)
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(formBody));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var requestId = document.Descendants().Single(element => element.Name.LocalName == "requestId").Value;
        var regionName = document.Descendants().Single(element => element.Name.LocalName == "regionName").Value;
        var regionEndpoint = document.Descendants().Single(element => element.Name.LocalName == "regionEndpoint").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(_context.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(requestId.Length, Is.EqualTo(RequestIdLength));
        Assert.That(regionName, Is.EqualTo(RegionName));
        Assert.That(regionEndpoint, Is.EqualTo(RegionEndpoint));
    }

    [TestCase("Action=DescribeInstances", "InvalidAction", "DescribeInstances")]
    [TestCase("Action=DescribeRegions&RegionName.1=not-a-region", "InvalidParameterValue", "not-a-region")]
    public async Task Test_That_We_Can_Answer_An_Error_In_The_Error_Element_Shape(string formBody, string expectedCode, string expectedDetail)
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(formBody));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var errorElement = document.Descendants().Single(element => element.Name.LocalName == "Error");
        var errorCode = errorElement.Descendants().Single(element => element.Name.LocalName == "Code").Value;
        var errorMessage = errorElement.Descendants().Single(element => element.Name.LocalName == "Message").Value;
        var errorRequestId = document.Descendants().Single(element => element.Name.LocalName == "RequestID").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(rootName, Is.EqualTo("Response"));
        Assert.That(errorCode, Is.EqualTo(expectedCode));
        Assert.That(errorMessage, Does.Contain(expectedDetail));
        Assert.That(errorRequestId.Length, Is.EqualTo(RequestIdLength));
    }
}
