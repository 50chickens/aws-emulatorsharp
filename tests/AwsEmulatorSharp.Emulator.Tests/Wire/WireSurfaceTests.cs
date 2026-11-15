using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Wire;

[TimedTestFixture(1)]
public class WireSurfaceTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string ResponseContentType = "text/xml";
    private const string UnknownActionRequest = "Action=DescribeInstances";
    private const string InvalidActionCode = "InvalidAction";
    private const string UnknownActionName = "DescribeInstances";
    private const int RequestIdLength = 32;

    private RecordingLogger _logger;
    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;

    [SetUp]
    public void SetUp()
    {
        var writer = new QueryResponseWriter(new WireConfiguration());
        var operations = new List<IQueryOperation>();

        _logger = new RecordingLogger();
        _surface = new WireSurface(operations, writer, _logger);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Can_Answer_An_Unknown_Action_In_The_Error_Element_Shape()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(UnknownActionRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var errorElement = document.Descendants().Single(element => element.Name.LocalName == "Error");
        var errorCode = errorElement.Descendants().Single(element => element.Name.LocalName == "Code").Value;
        var errorMessage = errorElement.Descendants().Single(element => element.Name.LocalName == "Message").Value;
        var errorRequestId = document.Descendants().Single(element => element.Name.LocalName == "RequestID").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(_context.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(rootName, Is.EqualTo("Response"));
        Assert.That(errorCode, Is.EqualTo(InvalidActionCode));
        Assert.That(errorMessage, Does.Contain(UnknownActionName));
        Assert.That(errorRequestId.Length, Is.EqualTo(RequestIdLength));
    }

    [Test]
    public async Task Test_That_We_Can_Log_The_Action_A_Request_Carries()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(UnknownActionRequest));

        await _surface.HandleAsync(_context);

        var entry = _logger.Entries.Single();

        Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
        Assert.That(entry.Message, Does.Contain(UnknownActionName));
    }
}
