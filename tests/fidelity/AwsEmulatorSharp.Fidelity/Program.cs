namespace AwsEmulatorSharp.Fidelity;

public static class Program
{
    private const string Usage = "usage: AwsEmulatorSharp.Fidelity <fidelity-report-path>";
    private const int PassedExitCode = 0;
    private const int FailedExitCode = 1;

    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine(Usage);

            return FailedExitCode;
        }

        var settings = new FidelitySettingsReader().Read();
        var gate = new FidelityGate(settings, new FidelityReportReader());
        var outcome = gate.Evaluate(args[0]);

        Console.WriteLine(outcome.GetSummary());

        return outcome.Passed ? PassedExitCode : FailedExitCode;
    }
}
