using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.S3;

public class S3ResponseWriter
{
    private const string ResponseNamespace = "http://s3.amazonaws.com/doc/2006-03-01/";
    private const string ResponseContentType = "application/xml";
    private const string ObjectContentType = "application/octet-stream";
    private const string ListBucketResultElementName = "ListBucketResult";
    private const string BucketNameElementName = "Name";
    private const string BucketElementName = "Bucket";
    private const string PrefixElementName = "Prefix";
    private const string KeyCountElementName = "KeyCount";
    private const string ContentsElementName = "Contents";
    private const string KeyElementName = "Key";
    private const string ETagElementName = "ETag";
    private const string SizeElementName = "Size";
    private const string UploadIdElementName = "UploadId";
    private const string InitiateMultipartUploadResultElementName = "InitiateMultipartUploadResult";
    private const string CompleteMultipartUploadResultElementName = "CompleteMultipartUploadResult";
    private const string ErrorElementName = "Error";
    private const string CodeElementName = "Code";
    private const string MessageElementName = "Message";

    public async Task WriteObjectAsync(HttpResponse response, StoredObject storedObject)
    {
        WriteObjectHeaders(response, storedObject);
        await response.Body.WriteAsync(storedObject.Content);
    }

    public void WriteHead(HttpResponse response, StoredObject storedObject)
    {
        WriteObjectHeaders(response, storedObject);
    }

    public Task WriteObjectListAsync(HttpResponse response, string bucketName, string prefix, IReadOnlyList<StoredObject> objects)
    {
        var document = BuildListDocument(bucketName, prefix, objects);

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public Task WriteUploadStartedAsync(HttpResponse response, string bucketName, string key, string uploadId)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var document = new XDocument(
            new XElement(xmlNamespace + InitiateMultipartUploadResultElementName,
                new XElement(xmlNamespace + BucketElementName, bucketName),
                new XElement(xmlNamespace + KeyElementName, key),
                new XElement(xmlNamespace + UploadIdElementName, uploadId)));

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public void WriteETag(HttpResponse response, string eTag)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.Headers.ETag = eTag;
    }

    public Task WriteUploadCompletedAsync(HttpResponse response, string bucketName, StoredObject storedObject)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var document = new XDocument(
            new XElement(xmlNamespace + CompleteMultipartUploadResultElementName,
                new XElement(xmlNamespace + BucketElementName, bucketName),
                new XElement(xmlNamespace + KeyElementName, storedObject.Key),
                new XElement(xmlNamespace + ETagElementName, storedObject.ETag)));

        return WriteDocumentAsync(response, StatusCodes.Status200OK, document);
    }

    public void WriteDeleted(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status204NoContent;
    }

    public Task WriteErrorAsync(HttpResponse response, int statusCode, string errorCode, string errorMessage)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var document = new XDocument(
            new XElement(xmlNamespace + ErrorElementName,
                new XElement(xmlNamespace + CodeElementName, errorCode),
                new XElement(xmlNamespace + MessageElementName, errorMessage)));

        return WriteDocumentAsync(response, statusCode, document);
    }

    private void WriteObjectHeaders(HttpResponse response, StoredObject storedObject)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = ObjectContentType;
        response.ContentLength = storedObject.Size;
        response.Headers.ETag = storedObject.ETag;
    }

    private XDocument BuildListDocument(string bucketName, string prefix, IReadOnlyList<StoredObject> objects)
    {
        var xmlNamespace = XNamespace.Get(ResponseNamespace);
        var contents = objects.Select(storedObject => BuildContentsElement(xmlNamespace, storedObject)).ToList();
        var document = new XDocument(
            new XElement(xmlNamespace + ListBucketResultElementName,
                new XElement(xmlNamespace + BucketNameElementName, bucketName),
                new XElement(xmlNamespace + PrefixElementName, prefix),
                new XElement(xmlNamespace + KeyCountElementName, objects.Count),
                contents));

        return document;
    }

    private XElement BuildContentsElement(XNamespace xmlNamespace, StoredObject storedObject)
    {
        var contentsElement = new XElement(xmlNamespace + ContentsElementName,
            new XElement(xmlNamespace + KeyElementName, storedObject.Key),
            new XElement(xmlNamespace + ETagElementName, storedObject.ETag),
            new XElement(xmlNamespace + SizeElementName, storedObject.Size));

        return contentsElement;
    }

    private Task WriteDocumentAsync(HttpResponse response, int statusCode, XDocument document)
    {
        response.StatusCode = statusCode;
        response.ContentType = ResponseContentType;

        return response.WriteAsync(document.ToString(), Encoding.UTF8);
    }
}
