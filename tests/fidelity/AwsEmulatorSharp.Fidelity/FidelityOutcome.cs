namespace AwsEmulatorSharp.Fidelity;

public class FidelityOutcome
{
    private const string PassedSummary = "fidelity gate passed";
    private const string FailedSummaryPrefix = "fidelity gate failed:";

    public FidelityOutcome(IReadOnlyList<string> failedCheckNames)
    {
        FailedCheckNames = failedCheckNames;
    }

    public IReadOnlyList<string> FailedCheckNames { get; }

    public bool Passed => FailedCheckNames.Count == 0;

    public string GetSummary()
    {
        if (Passed)
        {
            return PassedSummary;
        }

        var failedNames = string.Join(", ", FailedCheckNames);

        return $"{FailedSummaryPrefix} {failedNames}";
    }
}
