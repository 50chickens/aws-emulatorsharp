using Coop.Library.Squawk.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AwsEmulatorSharp.Core.Tests;

[SetUpFixture]
public class AssemblySetupFixture
{
    private ServiceProvider _provider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        services.AddSquawk();
        _provider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _provider.Dispose();
    }
}
