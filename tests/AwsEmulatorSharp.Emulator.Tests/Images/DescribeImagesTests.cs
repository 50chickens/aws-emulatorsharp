using System.Text;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Images;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Images;

[TimedTestFixture]
public class DescribeImagesTests
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string DescribeImagesRequest = "Action=DescribeImages";
    private const string UnknownImageId = "ami-00000000000000000";
    private const string InvalidImageIdNotFoundCode = "InvalidAMIID.NotFound";

    private WireSurface _surface;
    private DefaultHttpContext _context;
    private MemoryStream _responseBody;
    private Dictionary<string, string> _catalogue;
    private string _imageId;

    [SetUp]
    public void SetUp()
    {
        var configuration = new Ec2ConfigurationReader().Read();
        var writer = new QueryResponseWriter(new WireConfiguration());
        var operations = new List<IQueryOperation>
        {
            new DescribeImagesOperation(configuration, writer)
        };

        _catalogue = configuration.AmiToImage;
        _imageId = _catalogue.Keys.First();
        _surface = new WireSurface(operations, writer);
        _responseBody = new MemoryStream();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Post;
        _context.Request.ContentType = FormContentType;
        _context.Response.Body = _responseBody;
    }

    [Test]
    public async Task Test_That_We_Can_Describe_Every_Ami_In_The_Catalogue()
    {
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DescribeImagesRequest));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var rootName = document.Elements().Single().Name.LocalName;
        var imageIds = document.Descendants()
            .Where(element => element.Name.LocalName == "imageId")
            .Select(element => element.Value)
            .ToList();
        var imageLocations = document.Descendants()
            .Where(element => element.Name.LocalName == "imageLocation")
            .Select(element => element.Value)
            .ToList();

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(rootName, Is.EqualTo("DescribeImagesResponse"));
        Assert.That(imageIds, Is.EqualTo(_catalogue.Keys.ToList()));
        Assert.That(imageLocations, Is.EqualTo(_catalogue.Values.ToList()));
    }

    [Test]
    public async Task Test_That_We_Can_Describe_A_Named_Ami()
    {
        var request = $"Action=DescribeImages&ImageId.1={_imageId}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var imageIds = document.Descendants()
            .Where(element => element.Name.LocalName == "imageId")
            .Select(element => element.Value)
            .ToList();
        var imageLocation = document.Descendants().Single(element => element.Name.LocalName == "imageLocation").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(imageIds, Has.Count.EqualTo(1));
        Assert.That(imageIds, Does.Contain(_imageId));
        Assert.That(imageLocation, Is.EqualTo(_catalogue[_imageId]));
    }

    [Test]
    public async Task Test_That_We_Answer_Invalid_Ami_Id_Not_Found_For_An_Unnamed_Image()
    {
        var request = $"Action=DescribeImages&ImageId.1={UnknownImageId}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == "Code").Value;
        var errorMessage = document.Descendants().Single(element => element.Name.LocalName == "Message").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo(InvalidImageIdNotFoundCode));
        Assert.That(errorMessage, Does.Contain(UnknownImageId));
    }

    [Test]
    public async Task Test_That_We_Answer_Invalid_Ami_Id_Not_Found_When_One_Requested_Image_Is_Unnamed()
    {
        var request = $"Action=DescribeImages&ImageId.1={_imageId}&ImageId.2={UnknownImageId}";
        _context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request));

        await _surface.HandleAsync(_context);

        _responseBody.Position = 0;
        var document = XDocument.Load(_responseBody);
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == "Code").Value;

        Assert.That(_context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo(InvalidImageIdNotFoundCode));
    }
}
