using System.Text.Json;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Install;

[TimedTestFixture(1)]
public class ReportServerSettingsTests
{
    private const string ReportServerSettingsRelativePath = "installer\\report-server\\settings.json";
    private const string SqlServerSettingsRelativePath = "installer\\sql-server\\settings.json";
    private const string InstallSettingsRelativePath = "config\\install.json";
    private const string InstanceNamePropertyName = "instanceName";
    private const string EditionPropertyName = "edition";
    private const string NamespacePropertyName = "wmiNamespace";
    private const string CredentialsTypePropertyName = "databaseCredentialsType";
    private const string BaseUrlPropertyName = "baseUrl";
    private const string ApplicationsPropertyName = "applications";
    private const string ApplicationNamePropertyName = "name";
    private const string FeaturesPropertyName = "features";
    private const string ReportServerInstanceNamePropertyName = "reportServerInstanceName";
    private const string ReportServerEditionPropertyName = "reportServerEdition";
    private const string InstanceName = "PBIRS";
    private const string Edition = "DEV";
    private const string ReportServerNamespace = "root\\Microsoft\\SqlServer\\ReportServer\\RS_PBIRS\\V15\\Admin";
    private const string BaseUrl = "http://+:80";
    private const string FeatureSet = "SQLENGINE,FULLTEXT,IS,Conn,SDK";
    private const string ReportServerWebServiceApplication = "ReportServerWebService";
    private const string ReportServerWebAppApplication = "ReportServerWebApp";
    private const int DatabaseCredentialsType = 1;

    private JsonElement _reportServerSettings;
    private JsonElement _sqlServerSettings;
    private JsonElement _installSettings;

    [SetUp]
    public void SetUp()
    {
        var repositoryRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(repositoryRoot.FullName, ReportServerSettingsRelativePath)))
        {
            repositoryRoot = repositoryRoot.Parent!;
        }

        var reportServerSettingsPath = Path.Combine(repositoryRoot.FullName, ReportServerSettingsRelativePath);
        var sqlServerSettingsPath = Path.Combine(repositoryRoot.FullName, SqlServerSettingsRelativePath);
        var installSettingsPath = Path.Combine(repositoryRoot.FullName, InstallSettingsRelativePath);

        _reportServerSettings = JsonDocument.Parse(File.ReadAllText(reportServerSettingsPath)).RootElement;
        _sqlServerSettings = JsonDocument.Parse(File.ReadAllText(sqlServerSettingsPath)).RootElement;
        _installSettings = JsonDocument.Parse(File.ReadAllText(installSettingsPath)).RootElement;
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Name_The_Instance_That_Installed()
    {
        var instanceName = _reportServerSettings.GetProperty(InstanceNamePropertyName).GetString();

        Assert.That(instanceName, Is.EqualTo(InstanceName));
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Name_The_Edition_The_Bundle_Installs()
    {
        var edition = _reportServerSettings.GetProperty(EditionPropertyName).GetString();

        Assert.That(edition, Is.EqualTo(Edition));
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Name_The_Namespace_Of_The_Instance()
    {
        var reportServerNamespace = _reportServerSettings.GetProperty(NamespacePropertyName).GetString();

        Assert.That(reportServerNamespace, Is.EqualTo(ReportServerNamespace));
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Set_The_Database_Connection_To_Windows_Credentials()
    {
        var databaseCredentialsType = _reportServerSettings.GetProperty(CredentialsTypePropertyName).GetInt32();

        Assert.That(databaseCredentialsType, Is.EqualTo(DatabaseCredentialsType));
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Reserve_The_Base_Url()
    {
        var baseUrl = _reportServerSettings.GetProperty(BaseUrlPropertyName).GetString();

        Assert.That(baseUrl, Is.EqualTo(BaseUrl));
    }

    [Test]
    public void Test_That_The_Report_Server_Settings_Name_The_Service_And_Portal_Applications()
    {
        var applicationNames = _reportServerSettings.GetProperty(ApplicationsPropertyName).EnumerateArray()
            .Select(application => application.GetProperty(ApplicationNamePropertyName).GetString())
            .ToList();
        var expectedApplicationNames = new List<string?> { ReportServerWebServiceApplication, ReportServerWebAppApplication };

        Assert.That(applicationNames, Is.EqualTo(expectedApplicationNames));
    }

    [Test]
    public void Test_That_The_Sql_Server_Settings_Name_The_Feature_Set()
    {
        var features = _sqlServerSettings.GetProperty(FeaturesPropertyName).GetString();

        Assert.That(features, Is.EqualTo(FeatureSet));
    }

    [Test]
    public void Test_That_The_Install_Settings_Name_The_Instance_Whose_Database_The_Report_Server_Uses()
    {
        var reportServerInstanceName = _installSettings.GetProperty(ReportServerInstanceNamePropertyName).GetString();

        Assert.That(reportServerInstanceName, Is.EqualTo(InstanceName));
    }

    [Test]
    public void Test_That_The_Install_Settings_Name_The_Report_Server_Edition()
    {
        var reportServerEdition = _installSettings.GetProperty(ReportServerEditionPropertyName).GetString();

        Assert.That(reportServerEdition, Is.EqualTo(Edition));
    }
}
