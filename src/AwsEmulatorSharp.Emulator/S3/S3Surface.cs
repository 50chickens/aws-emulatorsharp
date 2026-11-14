using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.S3;

public class S3Surface
{
    private const char PathSeparator = '/';
    private const string GetMethod = "GET";
    private const string HeadMethod = "HEAD";
    private const string PutMethod = "PUT";
    private const string PostMethod = "POST";
    private const string DeleteMethod = "DELETE";
    private const string UploadsQueryName = "uploads";
    private const string UploadIdQueryName = "uploadId";
    private const string PartNumberQueryName = "partNumber";
    private const string PrefixQueryName = "prefix";
    private const string NoSuchKeyCode = "NoSuchKey";
    private const string NoSuchKeyMessageFormat = "The specified key '{0}' does not exist.";
    private const string InvalidRequestCode = "InvalidRequest";
    private const string InvalidRequestMessage = "The request is not an operation this emulator serves.";

    private readonly ObjectStore _store;
    private readonly S3ResponseWriter _writer;

    public S3Surface(ObjectStore store, S3ResponseWriter writer)
    {
        _store = store;
        _writer = writer;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var path = context.Request.Path.ToUriComponent();
        var segments = path.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var bucketName = segments.First();
        var key = string.Join(PathSeparator, segments.Skip(1));

        switch (context.Request.Method)
        {
            case GetMethod:
                await HandleGetAsync(context, bucketName, key);
                return;
            case HeadMethod:
                await HandleHeadAsync(context, bucketName, key);
                return;
            case PutMethod:
                await HandlePutAsync(context, bucketName, key);
                return;
            case PostMethod:
                await HandlePostAsync(context, bucketName, key);
                return;
            case DeleteMethod:
                HandleDelete(context, bucketName, key);
                return;
            default:
                await _writer.WriteErrorAsync(context.Response, StatusCodes.Status400BadRequest, InvalidRequestCode, InvalidRequestMessage);
                return;
        }
    }

    private async Task HandleGetAsync(HttpContext context, string bucketName, string key)
    {
        var isObjectRequest = key.Length > 0;
        if (!isObjectRequest)
        {
            await WriteObjectListAsync(context, bucketName);
            return;
        }

        await WriteObjectAsync(context, bucketName, key);
    }

    private async Task HandleHeadAsync(HttpContext context, string bucketName, string key)
    {
        var objects = _store.FindObject(bucketName, key);
        if (objects.Count == 0)
        {
            await WriteNoSuchKeyAsync(context.Response, key);
            return;
        }

        var storedObject = objects.First();
        _writer.WriteHead(context.Response, storedObject);
    }

    private async Task HandlePutAsync(HttpContext context, string bucketName, string key)
    {
        var partNumberValue = context.Request.Query[PartNumberQueryName].ToString();
        var isPartUpload = partNumberValue.Length > 0;
        if (isPartUpload)
        {
            await WritePartAsync(context, partNumberValue);
            return;
        }

        var content = await context.Request.ReadContentAsync();
        var storedObject = _store.PutObject(bucketName, key, content);
        _writer.WriteETag(context.Response, storedObject.ETag);
    }

    private async Task HandlePostAsync(HttpContext context, string bucketName, string key)
    {
        var uploadId = context.Request.Query[UploadIdQueryName].ToString();
        var isCompleteRequest = uploadId.Length > 0;
        if (isCompleteRequest)
        {
            await WriteUploadCompletedAsync(context, bucketName, uploadId);
            return;
        }

        var isStartRequest = context.Request.Query.ContainsKey(UploadsQueryName);
        if (isStartRequest)
        {
            await WriteUploadStartedAsync(context, bucketName, key);
            return;
        }

        await _writer.WriteErrorAsync(context.Response, StatusCodes.Status400BadRequest, InvalidRequestCode, InvalidRequestMessage);
    }

    private void HandleDelete(HttpContext context, string bucketName, string key)
    {
        _store.RemoveObject(bucketName, key);
        _writer.WriteDeleted(context.Response);
    }

    private async Task WriteObjectListAsync(HttpContext context, string bucketName)
    {
        var prefix = context.Request.Query[PrefixQueryName].ToString();
        var objects = _store.FindObjects(bucketName, prefix);
        await _writer.WriteObjectListAsync(context.Response, bucketName, prefix, objects);
    }

    private async Task WriteObjectAsync(HttpContext context, string bucketName, string key)
    {
        var objects = _store.FindObject(bucketName, key);
        if (objects.Count == 0)
        {
            await WriteNoSuchKeyAsync(context.Response, key);
            return;
        }

        var storedObject = objects.First();
        await _writer.WriteObjectAsync(context.Response, storedObject);
    }

    private async Task WritePartAsync(HttpContext context, string partNumberValue)
    {
        var uploadId = context.Request.Query[UploadIdQueryName].ToString();
        var partNumber = int.Parse(partNumberValue, CultureInfo.InvariantCulture);
        var content = await context.Request.ReadContentAsync();
        var eTag = _store.StorePart(uploadId, partNumber, content);
        _writer.WriteETag(context.Response, eTag);
    }

    private async Task WriteUploadStartedAsync(HttpContext context, string bucketName, string key)
    {
        var uploadId = _store.StartMultipartUpload(bucketName, key);
        await _writer.WriteUploadStartedAsync(context.Response, bucketName, key, uploadId);
    }

    private async Task WriteUploadCompletedAsync(HttpContext context, string bucketName, string uploadId)
    {
        var storedObject = _store.CompleteMultipartUpload(uploadId);
        await _writer.WriteUploadCompletedAsync(context.Response, bucketName, storedObject);
    }

    private async Task WriteNoSuchKeyAsync(HttpResponse response, string key)
    {
        var message = string.Format(NoSuchKeyMessageFormat, key);
        await _writer.WriteErrorAsync(response, StatusCodes.Status404NotFound, NoSuchKeyCode, message);
    }
}
