using AwsEmulatorSharp.Core;
using FiftyChickens.Library.Testing;
using NUnit.Framework;

namespace AwsEmulatorSharp.Core.Tests;

[TimedTestFixture]
public class ServiceEndpointTests
{
    private ServiceEndpoint _endpoint;

    [SetUp]
    public void SetUp()
    {
        _endpoint = new ServiceEndpoint("sqs", 4566);
    }

    [Test]
    public void Test_That_We_Can_Address_A_Service_On_The_Emulator()
    {
        var actual = _endpoint.ToUri();

        Assert.That(actual, Is.EqualTo(new Uri("http://localhost:4566/sqs")));
    }
}
