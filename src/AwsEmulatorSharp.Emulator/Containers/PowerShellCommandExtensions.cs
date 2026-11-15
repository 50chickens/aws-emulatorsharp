namespace AwsEmulatorSharp.Emulator.Containers;

internal static class PowerShellCommandExtensions
{
    private const string PowerShellExecutable = "powershell.exe";
    private const string NoProfileFlag = "-NoProfile";
    private const string ExecutionPolicyFlag = "-ExecutionPolicy";
    private const string BypassExecutionPolicy = "Bypass";
    private const string CommandFlag = "-Command";

    internal static List<string> ToPowerShellCommand(this string script)
    {
        var command = new List<string>
        {
            PowerShellExecutable,
            NoProfileFlag,
            ExecutionPolicyFlag,
            BypassExecutionPolicy,
            CommandFlag,
            script
        };

        return command;
    }
}
