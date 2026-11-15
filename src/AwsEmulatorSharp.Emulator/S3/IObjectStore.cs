namespace AwsEmulatorSharp.Emulator.S3;

public interface IObjectStore
{
    IReadOnlyList<StoredObject> FindObjects(string bucketName, string prefix);
}
