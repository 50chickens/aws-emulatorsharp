using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Identity;

public class AssumeRoleOperation : IQueryOperation
{
    private const string AssumeRoleActionName = "AssumeRole";
    private const string RoleArnFormName = "RoleArn";
    private const string RoleSessionNameFormName = "RoleSessionName";

    private readonly IdentityStore _store;
    private readonly IdentityResponseWriter _writer;

    public AssumeRoleOperation(IdentityStore store, IdentityResponseWriter writer)
    {
        _store = store;
        _writer = writer;
    }

    public string ActionName => AssumeRoleActionName;

    public Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var roleArn = form[RoleArnFormName].ToString();
        var roleSessionName = form[RoleSessionNameFormName].ToString();
        var session = _store.AssumeRole(roleArn, roleSessionName);

        return _writer.WriteAssumeRoleAsync(context.Response, session);
    }
}
