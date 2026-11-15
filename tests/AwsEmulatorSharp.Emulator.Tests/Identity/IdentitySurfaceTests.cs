using AwsEmulatorSharp.Emulator.Identity;
using AwsEmulatorSharp.Emulator.Wire;
using FiftyChickens.Library.Testing;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AwsEmulatorSharp.Emulator.Tests.Identity;

[TimedTestFixture]
public class IdentitySurfaceTests
{
    private const string AccessKey = "dummy-access-key";
    private const string AbsentAccessKey = "absent-access-key";
    private const string AbsentSessionToken = "absent-session-token";
    private const string Account = "000000000000";
    private const string CallerArn = "arn:aws:iam::000000000000:user/emulator";
    private const string RoleArn = "arn:aws:iam::000000000000:role/emulator";
    private const string RoleSessionName = "installer";
    private const string AssumedRoleArn = "arn:aws:sts::000000000000:assumed-role/emulator/installer";
    private const string ExpectedExpiration = "2026-11-15T18:07:34Z";
    private const string GetCallerIdentityResponseElementName = "GetCallerIdentityResponse";
    private const string AssumeRoleResponseElementName = "AssumeRoleResponse";
    private const string AccountElementName = "Account";
    private const string ArnElementName = "Arn";
    private const string CredentialsElementName = "Credentials";
    private const string AccessKeyIdElementName = "AccessKeyId";
    private const string SecretAccessKeyElementName = "SecretAccessKey";
    private const string SessionTokenElementName = "SessionToken";
    private const string ExpirationElementName = "Expiration";
    private const string ErrorCodeElementName = "Code";
    private const string InvalidClientTokenIdCode = "InvalidClientTokenId";
    private const string ResponseContentType = "text/xml";
    private const int CredentialElementCount = 4;

    private WireSurface _surface;
    private IdentityRequestFactory _requestFactory;
    private DateTimeOffset _fixedUtcNow;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _fixedUtcNow = new DateTimeOffset(2026, 11, 15, 17, 7, 34, TimeSpan.Zero);
    }

    [SetUp]
    public void SetUp()
    {
        var configuration = new IdentityConfigurationReader().Read();
        var store = new IdentityStore(configuration, new FixedTimeProvider(_fixedUtcNow));
        var writer = new IdentityResponseWriter();
        var operations = new List<IQueryOperation>
        {
            new GetCallerIdentityOperation(store, writer),
            new AssumeRoleOperation(store, writer)
        };

        _surface = new WireSurface(operations, new QueryResponseWriter(new WireConfiguration()), new RecordingLogger());
        _requestFactory = new IdentityRequestFactory();
    }

    [Test]
    public async Task Test_That_We_Answer_GetCallerIdentity_With_The_Account_And_Arn_The_Credential_Names()
    {
        var context = _requestFactory.CreateGetCallerIdentityRequest(AccessKey, string.Empty);

        await _surface.HandleAsync(context);

        var document = context.ReadResponseDocument();
        var rootName = document.Elements().Single().Name.LocalName;
        var account = document.Descendants().Single(element => element.Name.LocalName == AccountElementName).Value;
        var arn = document.Descendants().Single(element => element.Name.LocalName == ArnElementName).Value;

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(context.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(rootName, Is.EqualTo(GetCallerIdentityResponseElementName));
        Assert.That(account, Is.EqualTo(Account));
        Assert.That(arn, Is.EqualTo(CallerArn));
    }

    [Test]
    public async Task Test_That_We_Answer_InvalidClientTokenId_For_An_Access_Key_That_Names_No_Caller()
    {
        var context = _requestFactory.CreateGetCallerIdentityRequest(AbsentAccessKey, string.Empty);

        await _surface.HandleAsync(context);

        var document = context.ReadResponseDocument();
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == ErrorCodeElementName).Value;

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo(InvalidClientTokenIdCode));
    }

    [Test]
    public async Task Test_That_We_Answer_InvalidClientTokenId_For_A_Session_Token_That_Was_Not_Issued()
    {
        var context = _requestFactory.CreateGetCallerIdentityRequest(AccessKey, AbsentSessionToken);

        await _surface.HandleAsync(context);

        var document = context.ReadResponseDocument();
        var errorCode = document.Descendants().Single(element => element.Name.LocalName == ErrorCodeElementName).Value;

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(errorCode, Is.EqualTo(InvalidClientTokenIdCode));
    }

    [Test]
    public async Task Test_That_We_Answer_AssumeRole_With_Credentials_That_Expire()
    {
        var context = _requestFactory.CreateAssumeRoleRequest(AccessKey, RoleArn, RoleSessionName);

        await _surface.HandleAsync(context);

        var document = context.ReadResponseDocument();
        var rootName = document.Elements().Single().Name.LocalName;
        var credentials = document.Descendants().Single(element => element.Name.LocalName == CredentialsElementName);
        var credentialNames = credentials.Elements().Select(element => element.Name.LocalName).ToList();
        var expiration = credentials.Elements().Single(element => element.Name.LocalName == ExpirationElementName).Value;

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(context.Response.ContentType, Is.EqualTo(ResponseContentType));
        Assert.That(rootName, Is.EqualTo(AssumeRoleResponseElementName));
        Assert.That(credentialNames, Has.Count.EqualTo(CredentialElementCount));
        Assert.That(credentialNames, Does.Contain(AccessKeyIdElementName));
        Assert.That(credentialNames, Does.Contain(SecretAccessKeyElementName));
        Assert.That(credentialNames, Does.Contain(SessionTokenElementName));
        Assert.That(credentialNames, Does.Contain(ExpirationElementName));
        Assert.That(expiration, Is.EqualTo(ExpectedExpiration));
    }

    [Test]
    public async Task Test_That_We_Answer_The_Next_Call_With_The_Assumed_Role_Of_The_Session()
    {
        var assumeRoleContext = _requestFactory.CreateAssumeRoleRequest(AccessKey, RoleArn, RoleSessionName);
        await _surface.HandleAsync(assumeRoleContext);
        var assumeRoleDocument = assumeRoleContext.ReadResponseDocument();
        var credentials = assumeRoleDocument.Descendants().Single(element => element.Name.LocalName == CredentialsElementName);
        var accessKeyId = credentials.Elements().Single(element => element.Name.LocalName == AccessKeyIdElementName).Value;
        var sessionToken = credentials.Elements().Single(element => element.Name.LocalName == SessionTokenElementName).Value;

        var getCallerIdentityContext = _requestFactory.CreateGetCallerIdentityRequest(accessKeyId, sessionToken);
        await _surface.HandleAsync(getCallerIdentityContext);
        var getCallerIdentityDocument = getCallerIdentityContext.ReadResponseDocument();
        var account = getCallerIdentityDocument.Descendants().Single(element => element.Name.LocalName == AccountElementName).Value;
        var arn = getCallerIdentityDocument.Descendants().Single(element => element.Name.LocalName == ArnElementName).Value;

        Assert.That(getCallerIdentityContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(account, Is.EqualTo(Account));
        Assert.That(arn, Is.EqualTo(AssumedRoleArn));
    }
}
