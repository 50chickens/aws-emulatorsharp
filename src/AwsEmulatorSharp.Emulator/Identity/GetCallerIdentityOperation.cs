using AwsEmulatorSharp.Emulator.Auth;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Identity;

public class GetCallerIdentityOperation : IQueryOperation
{
    private const string GetCallerIdentityActionName = "GetCallerIdentity";
    private const string SessionTokenHeaderName = "X-Amz-Security-Token";
    private const string InvalidClientTokenIdCode = "InvalidClientTokenId";
    private const string InvalidClientTokenIdMessage = "The security token included in the request is invalid.";

    private readonly IdentityStore _store;
    private readonly IdentityResponseWriter _writer;

    public GetCallerIdentityOperation(IdentityStore store, IdentityResponseWriter writer)
    {
        _store = store;
        _writer = writer;
    }

    public string ActionName => GetCallerIdentityActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var credential = context.Request.ReadRequestCredential();
        var sessionToken = context.Request.Headers[SessionTokenHeaderName].ToString();
        var callers = _store.FindCaller(credential.AccessKey, sessionToken);

        if (callers.Count == 0)
        {
            await _writer.WriteErrorAsync(context.Response, InvalidClientTokenIdCode, InvalidClientTokenIdMessage);
            return;
        }

        var caller = callers.First();
        await _writer.WriteGetCallerIdentityAsync(context.Response, caller);
    }
}
