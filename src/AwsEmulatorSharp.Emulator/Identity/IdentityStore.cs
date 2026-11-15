using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;

namespace AwsEmulatorSharp.Emulator.Identity;

public class IdentityStore
{
    private const string RoleNameSeparator = "/";
    private const string SessionAddressSeparator = ":";
    private const string AssumedRoleArnFormat = "arn:aws:sts::{0}:assumed-role/{1}/{2}";
    private const string SessionAccessKeyPrefix = "ASIA";
    private const int SessionAccessKeyLength = 16;
    private const int SecretAccessKeyLength = 40;
    private const int SessionTokenLength = 64;

    private readonly IdentityConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, AssumedRoleSession> _sessions = new(StringComparer.Ordinal);

    public IdentityStore(IdentityConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<IdentityCaller> FindCaller(string accessKey, string sessionToken)
    {
        if (sessionToken.Length > 0)
        {
            return FindSessionCaller(accessKey, sessionToken);
        }

        return FindConfiguredCaller(accessKey);
    }

    public AssumedRoleSession AssumeRole(string roleArn, string roleSessionName)
    {
        var accessKeyId = SessionAccessKeyPrefix + RandomNumberGenerator.GetHexString(SessionAccessKeyLength, true).ToUpperInvariant();
        var secretAccessKey = RandomNumberGenerator.GetHexString(SecretAccessKeyLength, true);
        var sessionToken = RandomNumberGenerator.GetHexString(SessionTokenLength, true);
        var roleName = roleArn.Split(RoleNameSeparator)[^1];
        var assumedRoleArn = string.Format(CultureInfo.InvariantCulture, AssumedRoleArnFormat, _configuration.AccountId, roleName, roleSessionName);
        var expiration = _timeProvider.GetUtcNow().AddSeconds(_configuration.SessionDurationSeconds);
        var session = new AssumedRoleSession(accessKeyId, secretAccessKey, sessionToken, assumedRoleArn, expiration);
        var sessionAddress = $"{accessKeyId}{SessionAddressSeparator}{sessionToken}";
        _sessions[sessionAddress] = session;

        return session;
    }

    private IReadOnlyList<IdentityCaller> FindConfiguredCaller(string accessKey)
    {
        if (accessKey != _configuration.AccessKeyId)
        {
            var absentCallers = new List<IdentityCaller>();

            return absentCallers;
        }

        var callers = new List<IdentityCaller> { new IdentityCaller(_configuration.AccountId, _configuration.CallerArn) };

        return callers;
    }

    private IReadOnlyList<IdentityCaller> FindSessionCaller(string accessKey, string sessionToken)
    {
        var sessionAddress = $"{accessKey}{SessionAddressSeparator}{sessionToken}";
        if (!_sessions.TryGetValue(sessionAddress, out var session))
        {
            var absentCallers = new List<IdentityCaller>();

            return absentCallers;
        }

        var callers = new List<IdentityCaller> { new IdentityCaller(_configuration.AccountId, session.AssumedRoleArn) };

        return callers;
    }
}
