using AwsEmulatorSharp.Emulator.Auth;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2;
using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Images;
using AwsEmulatorSharp.Emulator.S3;
using AwsEmulatorSharp.Emulator.Secrets;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests;

[TimedTestFixture(1)]
public class DependencyResolverTests
{
    private IServiceCollection _services;
    private ServiceProvider _provider;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        _services.AddContainersModule();
        _services.AddWireModule();
        _services.AddAuthModule();
        _services.AddImagesModule();
        _services.AddEc2Module();
        _services.AddS3Module();
        _services.AddIdentityModule();
        _services.AddSecretsModule();

        _provider = _services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [Test]
    public void That_Every_Registered_Service_Resolves()
    {
        var actual = _services
            .Select(descriptor => _provider.GetServices(descriptor.ServiceType))
            .ToList();

        Assert.That(actual, Has.Count.EqualTo(_services.Count));
    }
}
