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

[TimedTestFixture]
public class DependencyResolverTests
{
    private int _registeredCount;
    private ServiceProvider _provider;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddContainersModule();
        services.AddWireModule();
        services.AddAuthModule();
        services.AddImagesModule();
        services.AddEc2Module();
        services.AddS3Module();
        services.AddIdentityModule();
        services.AddSecretsModule();

        _registeredCount = services.Count;
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [Test]
    public void That_Every_Registered_Service_Resolves()
    {
        var actual = _provider.GetServices(typeof(object)).ToList();

        Assert.That(actual, Has.Count.EqualTo(_registeredCount));
    }
}
