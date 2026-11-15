namespace AwsEmulatorSharp.Emulator.Install;

public class ReportServerInstallStep : InstallStep
{
    public ReportServerInstallStep(string name, string installerKey, string installerUrl, string commandLine, string databaseName)
        : base(name, installerKey, installerUrl, commandLine)
    {
        DatabaseName = databaseName;
    }

    public string DatabaseName { get; }
}
