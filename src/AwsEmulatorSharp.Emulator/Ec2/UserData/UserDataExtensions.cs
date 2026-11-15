using System.Text;
using System.Text.RegularExpressions;

namespace AwsEmulatorSharp.Emulator.Ec2.UserData;

internal static class UserDataExtensions
{
    private const string RebootRequestPattern = @"(?im)^[ \t]*Restart-Computer\b[^\r\n]*(?:\r?\n)?";

    internal static string DecodeUserData(this string encodedUserData)
    {
        var decodedBytes = Convert.FromBase64String(encodedUserData);
        var script = Encoding.UTF8.GetString(decodedBytes);

        return script;
    }

    internal static IReadOnlyList<UserDataStage> ToStages(this string script)
    {
        var rebootRequestEnds = Regex
            .Matches(script, RebootRequestPattern)
            .Select(match => match.Index + match.Length)
            .ToList();
        var stageStarts = rebootRequestEnds.Prepend(0).ToList();
        var stages = rebootRequestEnds
            .Zip(stageStarts, (rebootRequestEnd, stageStart) => new UserDataStage(script.Substring(stageStart, rebootRequestEnd - stageStart).TrimEnd(), true))
            .ToList();
        var remainingStage = script.Substring(rebootRequestEnds.LastOrDefault());
        if (remainingStage.Trim().Length > 0)
        {
            stages.Add(new UserDataStage(remainingStage, false));
        }

        return stages;
    }

    internal static string AppendOutput(this string consoleOutput, string stageOutput)
    {
        var output = stageOutput;
        if (consoleOutput.Length > 0)
        {
            output = consoleOutput + Environment.NewLine + stageOutput;
        }

        return output;
    }
}
