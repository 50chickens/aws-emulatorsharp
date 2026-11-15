namespace AwsEmulatorSharp.Emulator.Identity;

public class IdentityCaller
{
    public IdentityCaller(string account, string arn)
    {
        Account = account;
        Arn = arn;
    }

    public string Account { get; }

    public string Arn { get; }
}
