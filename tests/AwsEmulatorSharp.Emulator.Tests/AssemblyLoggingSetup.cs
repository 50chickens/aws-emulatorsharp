using System.Runtime.Versioning;
using Coop.Library.Squawk.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[SetUpFixture]
public class AssemblyLoggingSetup
{
    private ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    [SupportedOSPlatform("windows")]
    public void BuildContainer()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSquawk(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void DisposeContainer()
    {
        _serviceProvider.Dispose();
    }
}
