using System.Text;
using AwsEmulatorSharp.Emulator.S3;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.S3;

[TimedTestFixture]
public class S3SurfaceTests
{
    private const string GetMethod = "GET";
    private const string HeadMethod = "HEAD";
    private const string PutMethod = "PUT";
    private const string PostMethod = "POST";
    private const string DeleteMethod = "DELETE";
    private const string BucketName = "installer-media";
    private const string BucketPath = "/installer-media";
    private const string Prefix = "installer/";
    private const string ListPrefixQueryString = "?prefix=" + Prefix;
    private const string SinglePutKey = "installer/single-put.iso";
    private const string SinglePutPath = "/installer-media/installer/single-put.iso";
    private const string MultipartKey = "installer/multipart.iso";
    private const string MultipartPath = "/installer-media/installer/multipart.iso";
    private const string SecondMediaKey = "installer/agent.msi";
    private const string SecondMediaPath = "/installer-media/installer/agent.msi";
    private const string ArchiveKey = "archive/gateway.iso";
    private const string ArchivePath = "/installer-media/archive/gateway.iso";
    private const string MediaBody = "gateway installer media bytes";
    private const string SecondMediaBody = "agent installer media bytes";
    private const string ArchiveMediaBody = "archived installer media bytes";
    private const string UploadsQueryString = "?uploads";
    private const string PartNumberQueryName = "partNumber";
    private const string UploadIdQueryName = "uploadId";
    private const string PresignedQueryString = "?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=dummy-access-key%2F20240101%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240101T000000Z&X-Amz-SignedHeaders=host&X-Amz-Signature=0f8b1a2c3d4e5f60";
    private const string ContentsElementName = "Contents";
    private const string KeyElementName = "Key";
    private const string SizeElementName = "Size";
    private const string ETagElementName = "ETag";
    private const string UploadIdElementName = "UploadId";
    private const string CodeElementName = "Code";
    private const string NoSuchKeyCode = "NoSuchKey";
    private const int FirstPartLength = 9;
    private const int FirstPartNumber = 1;
    private const int SecondPartNumber = 2;

    private byte[] _mediaContent;
    private byte[] _secondMediaContent;
    private byte[] _archiveMediaContent;
    private S3Surface _surface;
    private S3RequestFactory _requestFactory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mediaContent = Encoding.UTF8.GetBytes(MediaBody);
        _secondMediaContent = Encoding.UTF8.GetBytes(SecondMediaBody);
        _archiveMediaContent = Encoding.UTF8.GetBytes(ArchiveMediaBody);
    }

    [SetUp]
    public void SetUp()
    {
        var store = new ObjectStore();
        var writer = new S3ResponseWriter();
        _surface = new S3Surface(store, writer);
        _requestFactory = new S3RequestFactory();
    }

    [Test]
    public async Task Test_That_We_Read_The_Same_Bytes_We_Stored()
    {
        var putContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(putContext);
        var storedETag = putContext.Response.Headers.ETag.ToString();

        var getContext = _requestFactory.CreateRequest(GetMethod, SinglePutPath, string.Empty);
        await _surface.HandleAsync(getContext);
        var content = getContext.ReadResponseContent();

        Assert.That(putContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(getContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(content, Is.EqualTo(_mediaContent));
        Assert.That(getContext.Response.Headers.ETag.ToString(), Is.EqualTo(storedETag));
    }

    [Test]
    public async Task Test_That_We_Head_An_Object_And_Read_Its_Size_And_ETag()
    {
        var putContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(putContext);
        var storedETag = putContext.Response.Headers.ETag.ToString();

        var headContext = _requestFactory.CreateRequest(HeadMethod, SinglePutPath, string.Empty);
        await _surface.HandleAsync(headContext);

        Assert.That(headContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(headContext.Response.ContentLength, Is.EqualTo((long)_mediaContent.Length));
        Assert.That(headContext.Response.Headers.ETag.ToString(), Is.EqualTo(storedETag));
    }

    [Test]
    public async Task Test_That_We_List_Objects_By_Prefix_With_Size_And_ETag()
    {
        var mediaPutContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(mediaPutContext);
        var secondMediaPutContext = _requestFactory.CreateRequest(PutMethod, SecondMediaPath, _secondMediaContent, string.Empty);
        await _surface.HandleAsync(secondMediaPutContext);
        var archivePutContext = _requestFactory.CreateRequest(PutMethod, ArchivePath, _archiveMediaContent, string.Empty);
        await _surface.HandleAsync(archivePutContext);

        var listContext = _requestFactory.CreateRequest(GetMethod, BucketPath, ListPrefixQueryString);
        await _surface.HandleAsync(listContext);
        var document = listContext.ReadResponseDocument();
        var contentsElements = document.Descendants().Where(element => element.Name.LocalName == ContentsElementName).ToList();
        var listedKeys = contentsElements.Select(element => element.Descendants().Single(child => child.Name.LocalName == KeyElementName).Value).ToList();
        var listedSizes = contentsElements.Select(element => element.Descendants().Single(child => child.Name.LocalName == SizeElementName).Value).ToList();
        var listedETags = contentsElements.Select(element => element.Descendants().Single(child => child.Name.LocalName == ETagElementName).Value).ToList();

        Assert.That(listedKeys, Is.EqualTo(new List<string> { SecondMediaKey, SinglePutKey }));
        Assert.That(listedSizes, Is.EqualTo(new List<string> { _secondMediaContent.Length.ToString(), _mediaContent.Length.ToString() }));
        Assert.That(listedETags, Is.EqualTo(new List<string> { secondMediaPutContext.Response.Headers.ETag.ToString(), mediaPutContext.Response.Headers.ETag.ToString() }));
    }

    [Test]
    public async Task Test_That_We_Answer_A_Presigned_Get_Without_An_Authorization_Header()
    {
        var putContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(putContext);

        var presignedContext = _requestFactory.CreateRequest(GetMethod, SinglePutPath, PresignedQueryString);
        await _surface.HandleAsync(presignedContext);
        var content = presignedContext.ReadResponseContent();

        Assert.That(presignedContext.Request.Headers.Authorization.ToString(), Is.EqualTo(string.Empty));
        Assert.That(presignedContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(content, Is.EqualTo(_mediaContent));
    }

    [Test]
    public async Task Test_That_We_Assemble_A_Multipart_Upload_Into_The_Bytes_Of_One_Put()
    {
        var putContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(putContext);

        var startContext = _requestFactory.CreateRequest(PostMethod, MultipartPath, UploadsQueryString);
        await _surface.HandleAsync(startContext);
        var startDocument = startContext.ReadResponseDocument();
        var uploadId = startDocument.Descendants().Single(element => element.Name.LocalName == UploadIdElementName).Value;

        var firstPart = _mediaContent.Take(FirstPartLength).ToArray();
        var secondPart = _mediaContent.Skip(FirstPartLength).ToArray();
        var firstPartQueryString = $"?{PartNumberQueryName}={FirstPartNumber}&{UploadIdQueryName}={uploadId}";
        var secondPartQueryString = $"?{PartNumberQueryName}={SecondPartNumber}&{UploadIdQueryName}={uploadId}";
        await _surface.HandleAsync(_requestFactory.CreateRequest(PutMethod, MultipartPath, firstPart, firstPartQueryString));
        await _surface.HandleAsync(_requestFactory.CreateRequest(PutMethod, MultipartPath, secondPart, secondPartQueryString));

        var completeQueryString = $"?{UploadIdQueryName}={uploadId}";
        var completeContext = _requestFactory.CreateRequest(PostMethod, MultipartPath, completeQueryString);
        await _surface.HandleAsync(completeContext);

        var singlePutContext = _requestFactory.CreateRequest(GetMethod, SinglePutPath, string.Empty);
        await _surface.HandleAsync(singlePutContext);
        var multipartContext = _requestFactory.CreateRequest(GetMethod, MultipartPath, string.Empty);
        await _surface.HandleAsync(multipartContext);

        Assert.That(completeContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(multipartContext.ReadResponseContent(), Is.EqualTo(singlePutContext.ReadResponseContent()));
    }

    [Test]
    public async Task Test_That_We_Answer_NoSuchKey_After_A_Delete()
    {
        var putContext = _requestFactory.CreateRequest(PutMethod, SinglePutPath, _mediaContent, string.Empty);
        await _surface.HandleAsync(putContext);

        var deleteContext = _requestFactory.CreateRequest(DeleteMethod, SinglePutPath, string.Empty);
        await _surface.HandleAsync(deleteContext);

        var getContext = _requestFactory.CreateRequest(GetMethod, SinglePutPath, string.Empty);
        await _surface.HandleAsync(getContext);
        var document = getContext.ReadResponseDocument();
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == CodeElementName).Value;

        Assert.That(deleteContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
        Assert.That(getContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(errorCode, Is.EqualTo(NoSuchKeyCode));
    }
}
