using System.Text;

namespace AwsEmulatorSharp.Emulator.Ec2.UserData;

internal static class UserDataExtensions
{
    internal static string DecodeUserData(this string encodedUserData)
    {
        var decodedBytes = Convert.FromBase64String(encodedUserData);
        var script = Encoding.UTF8.GetString(decodedBytes);

        return script;
    }
}
