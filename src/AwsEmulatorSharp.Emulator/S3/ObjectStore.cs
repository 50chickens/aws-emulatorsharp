using System.Collections.Concurrent;

namespace AwsEmulatorSharp.Emulator.S3;

public class ObjectStore : IObjectStore
{
    private const string AddressSeparator = "/";

    private readonly ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, MultipartUploadState> _uploads = new(StringComparer.Ordinal);

    public StoredObject PutObject(string bucketName, string key, byte[] content)
    {
        var eTag = content.ToETag();
        var storedObject = new StoredObject(key, content, eTag);
        var address = ToAddress(bucketName, key);
        _objects[address] = storedObject;

        return storedObject;
    }

    public IReadOnlyList<StoredObject> FindObject(string bucketName, string key)
    {
        var address = ToAddress(bucketName, key);
        if (!_objects.TryGetValue(address, out var storedObject))
        {
            var absentObjects = new List<StoredObject>();

            return absentObjects;
        }

        var foundObjects = new List<StoredObject> { storedObject };

        return foundObjects;
    }

    public IReadOnlyList<StoredObject> FindObjects(string bucketName, string prefix)
    {
        var bucketAddress = ToAddress(bucketName, string.Empty);
        var objects = _objects
            .Where(pair => pair.Key.StartsWith(bucketAddress, StringComparison.Ordinal))
            .Select(pair => pair.Value)
            .Where(storedObject => storedObject.Key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(storedObject => storedObject.Key, StringComparer.Ordinal)
            .ToList();

        return objects;
    }

    public void RemoveObject(string bucketName, string key)
    {
        var address = ToAddress(bucketName, key);
        _objects.TryRemove(address, out _);
    }

    public string StartMultipartUpload(string bucketName, string key)
    {
        var uploadId = Guid.NewGuid().ToString();
        var upload = new MultipartUploadState(bucketName, key);
        _uploads[uploadId] = upload;

        return uploadId;
    }

    public string StorePart(string uploadId, int partNumber, byte[] content)
    {
        var upload = _uploads[uploadId];
        upload.AddPart(partNumber, content);

        return content.ToETag();
    }

    public StoredObject CompleteMultipartUpload(string uploadId)
    {
        var upload = _uploads[uploadId];
        var content = upload.ToContent();
        _uploads.TryRemove(uploadId, out _);
        var storedObject = PutObject(upload.BucketName, upload.Key, content);

        return storedObject;
    }

    private string ToAddress(string bucketName, string key)
    {
        var address = $"{bucketName}{AddressSeparator}{key}";

        return address;
    }
}
