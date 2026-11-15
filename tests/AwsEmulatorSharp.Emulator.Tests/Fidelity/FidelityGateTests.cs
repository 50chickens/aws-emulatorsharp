using AwsEmulatorSharp.Fidelity;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Fidelity;

[TimedTestFixture(1)]
public class FidelityGateTests
{
    private const string ReportsFolderName = "Fidelity\\Reports";
    private const string AllChecksPassedReportFileName = "recorded-run-all-checks-passed.json";
    private const string AllChecksPassedSummary = "fidelity gate passed";
    private const string TwoChecksFailedReportFileName = "recorded-run-two-checks-failed.json";
    private const string TwoChecksFailedSummary = "fidelity gate failed: ec2-describe-instance-status, ec2-get-console-output";
    private const string CheckMissingReportFileName = "recorded-run-check-missing.json";
    private const string CheckMissingSummary = "fidelity gate failed: ec2-describe-instance-status";
    private const string RunInstancesCheckName = "ec2-run-instances";
    private const string DescribeInstancesCheckName = "ec2-describe-instances";
    private const string DescribeInstanceStatusCheckName = "ec2-describe-instance-status";
    private const string GetConsoleOutputCheckName = "ec2-get-console-output";
    private const string TerminateInstancesCheckName = "ec2-terminate-instances";

    private FidelityGate _gate;
    private string _reportsFolderPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _reportsFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, ReportsFolderName);
    }

    [SetUp]
    public void SetUp()
    {
        var requiredCheckNames = new List<string>
        {
            RunInstancesCheckName,
            DescribeInstancesCheckName,
            DescribeInstanceStatusCheckName,
            GetConsoleOutputCheckName,
            TerminateInstancesCheckName
        };
        var settings = new FidelitySettings { RequiredCheckNames = requiredCheckNames };

        _gate = new FidelityGate(settings, new FidelityReportReader());
    }

    [TestCase(AllChecksPassedReportFileName, AllChecksPassedSummary, true)]
    [TestCase(TwoChecksFailedReportFileName, TwoChecksFailedSummary, false)]
    [TestCase(CheckMissingReportFileName, CheckMissingSummary, false)]
    public void Test_That_The_Gate_Names_The_Checks_A_Recorded_Report_Does_Not_Mark_Passed(string reportFileName, string expectedSummary, bool expectedPassed)
    {
        var reportPath = Path.Combine(_reportsFolderPath, reportFileName);
        var outcome = _gate.Evaluate(reportPath);

        Assert.That(outcome.GetSummary(), Is.EqualTo(expectedSummary));
        Assert.That(outcome.Passed, Is.EqualTo(expectedPassed));
    }
}
