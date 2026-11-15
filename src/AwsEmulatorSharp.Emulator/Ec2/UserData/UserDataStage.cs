namespace AwsEmulatorSharp.Emulator.Ec2.UserData;

public class UserDataStage
{
    public UserDataStage(string script, bool requestsReboot)
    {
        Script = script;
        RequestsReboot = requestsReboot;
    }

    public string Script { get; }

    public bool RequestsReboot { get; }
}
