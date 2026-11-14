using System.Security.Cryptography;

namespace AwsEmulatorSharp.Emulator.S3;

internal static class ObjectContentExtensions
{
    private const string ETagFormat = "\"{0}\"";

    internal static string ToETag(this byte[] content)
    {
        var hash = MD5.HashData(content);
        var hashText = Convert.ToHexString(hash).ToLowerInvariant();
        var eTag = string.Format(ETagFormat, hashText);

        return eTag;
    }
}
