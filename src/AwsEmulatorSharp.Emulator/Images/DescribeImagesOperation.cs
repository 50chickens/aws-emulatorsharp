using System.Globalization;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Ec2.Describe;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Images;

public class DescribeImagesOperation : IQueryOperation
{
    private const string ImagesActionName = "DescribeImages";
    private const string ImageIdParameterPrefix = "ImageId.";
    private const string InvalidImageIdNotFoundCode = "InvalidAMIID.NotFound";
    private const string ImageNotFoundMessageFormat = "The image id '[{0}]' does not exist";
    private const string ResponseRootElementName = "DescribeImagesResponse";
    private const string RequestIdElementName = "requestId";
    private const string ImagesSetElementName = "imagesSet";
    private const string ItemElementName = "item";
    private const string ImageIdElementName = "imageId";
    private const string ImageLocationElementName = "imageLocation";
    private const string ImageStateElementName = "imageState";
    private const string CreationDateElementName = "creationDate";
    private const string OwnerIdElementName = "ownerId";
    private const string AvailableImageStateName = "available";

    private readonly Ec2Configuration _configuration;
    private readonly QueryResponseWriter _writer;

    public DescribeImagesOperation(Ec2Configuration configuration, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _writer = writer;
    }

    public string ActionName => ImagesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var requestedImageIds = form.ReadValues(ImageIdParameterPrefix);
        var unknownImageIds = requestedImageIds
            .Where(imageId => !_configuration.AmiToImage.ContainsKey(imageId))
            .ToList();

        if (unknownImageIds.Count > 0)
        {
            var unknownImageId = unknownImageIds.First();
            var message = string.Format(CultureInfo.InvariantCulture, ImageNotFoundMessageFormat, unknownImageId);
            await _writer.WriteErrorAsync(context.Response, InvalidImageIdNotFoundCode, message);
            return;
        }

        await _writer.WriteAsync(context.Response, BuildDocument(requestedImageIds));
    }

    private XDocument BuildDocument(List<string> requestedImageIds)
    {
        var xmlNamespace = _writer.XmlNamespace;
        var imageIds = _configuration.AmiToImage.Keys.ToList().SelectRequested(requestedImageIds, imageId => imageId);

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + ImagesSetElementName,
                    imageIds.Select(imageId => BuildItem(xmlNamespace, imageId)))));
    }

    private XElement BuildItem(XNamespace xmlNamespace, string imageId)
    {
        return new XElement(xmlNamespace + ItemElementName,
            new XElement(xmlNamespace + ImageIdElementName, imageId),
            new XElement(xmlNamespace + ImageLocationElementName, _configuration.AmiToImage[imageId]),
            new XElement(xmlNamespace + ImageStateElementName, AvailableImageStateName),
            new XElement(xmlNamespace + CreationDateElementName, _configuration.ImageCreationDate),
            new XElement(xmlNamespace + OwnerIdElementName, _configuration.ImageOwnerId));
    }
}
