namespace AwsEmulatorSharp.Emulator.S3;

public class StoredObject
{
    public StoredObject(string key, byte[] content, string eTag)
    {
        Key = key;
        Content = content;
        ETag = eTag;
    }

    public string Key { get; }

    public byte[] Content { get; }

    public string ETag { get; }

    public int Size => Content.Length;
}
