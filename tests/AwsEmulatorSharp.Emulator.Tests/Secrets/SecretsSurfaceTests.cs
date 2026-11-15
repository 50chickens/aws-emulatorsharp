using AwsEmulatorSharp.Emulator.Secrets;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Secrets;

[TimedTestFixture(1)]
public class SecretsSurfaceTests
{
    private const string SecretName = "installer/database-password";
    private const string SecretText = "correct-horse-battery-staple";
    private const string UnknownSecretName = "installer/absent-secret";
    private const string SecretStringElementName = "SecretString";
    private const string VersionIdElementName = "VersionId";
    private const string VersionStagesElementName = "VersionStages";
    private const string ErrorTypeElementName = "__type";
    private const string CurrentVersionStage = "AWSCURRENT";
    private const string ResourceNotFoundErrorType = "ResourceNotFoundException";
    private const string ResponseContentType = "application/x-amz-json-1.1";

    private SecretsSurface _surface;
    private SecretsRequestFactory _requestFactory;

    [SetUp]
    public void SetUp()
    {
        var store = new SecretStore();
        var writer = new SecretsResponseWriter();
        _surface = new SecretsSurface(store, writer);
        _requestFactory = new SecretsRequestFactory();
    }

    [Test]
    public async Task Test_That_We_Read_The_Secret_We_Put()
    {
        var putContext = _requestFactory.CreatePutSecretValueRequest(SecretName, SecretText);
        await _surface.HandleAsync(putContext);
        using var putDocument = putContext.ReadResponseDocument();
        var putVersionId = putDocument.RootElement.GetProperty(VersionIdElementName).ToString();

        var getContext = _requestFactory.CreateGetSecretValueRequest(SecretName);
        await _surface.HandleAsync(getContext);
        using var getDocument = getContext.ReadResponseDocument();
        var secretString = getDocument.RootElement.GetProperty(SecretStringElementName).ToString();
        var getVersionId = getDocument.RootElement.GetProperty(VersionIdElementName).ToString();
        var versionStage = getDocument.RootElement.GetProperty(VersionStagesElementName)[0].ToString();

        Assert.That(putContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(getContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(getContext.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(secretString, Is.EqualTo(SecretText));
        Assert.That(getVersionId, Is.EqualTo(putVersionId));
        Assert.That(versionStage, Is.EqualTo(CurrentVersionStage));
    }

    [Test]
    public async Task Test_That_We_Answer_ResourceNotFound_For_A_Secret_That_Was_Not_Put()
    {
        var getContext = _requestFactory.CreateGetSecretValueRequest(UnknownSecretName);
        await _surface.HandleAsync(getContext);
        using var document = getContext.ReadResponseDocument();
        var errorType = document.RootElement.GetProperty(ErrorTypeElementName).ToString();

        Assert.That(getContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorType, Is.EqualTo(ResourceNotFoundErrorType));
    }
}
