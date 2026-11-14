namespace AwsEmulatorSharp.Emulator.S3;

internal class MultipartUploadState
{
    private readonly SortedDictionary<int, byte[]> _parts = new();

    internal MultipartUploadState(string bucketName, string key)
    {
        BucketName = bucketName;
        Key = key;
    }

    internal string BucketName { get; }

    internal string Key { get; }

    internal void AddPart(int partNumber, byte[] content)
    {
        _parts[partNumber] = content;
    }

    internal byte[] ToContent()
    {
        var content = _parts.Values.SelectMany(part => part).ToArray();

        return content;
    }
}
