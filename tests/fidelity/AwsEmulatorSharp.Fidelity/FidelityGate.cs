namespace AwsEmulatorSharp.Fidelity;

public class FidelityGate
{
    private readonly FidelitySettings _settings;
    private readonly FidelityReportReader _reportReader;

    public FidelityGate(FidelitySettings settings, FidelityReportReader reportReader)
    {
        _settings = settings;
        _reportReader = reportReader;
    }

    public FidelityOutcome Evaluate(string reportPath)
    {
        var report = _reportReader.Read(reportPath);
        var failedCheckNames = _settings.RequiredCheckNames
            .Where(checkName => !report.IsPassed(checkName))
            .ToList();

        return new FidelityOutcome(failedCheckNames);
    }
}
