namespace AwsEmulatorSharp.Fidelity;

public static class FidelityReportExtensions
{
    public static bool IsPassed(this FidelityReport report, string checkName)
    {
        var check = report.Checks.FirstOrDefault(result => result.Name == checkName);
        if (check == null)
        {
            return false;
        }

        return check.Passed;
    }
}
