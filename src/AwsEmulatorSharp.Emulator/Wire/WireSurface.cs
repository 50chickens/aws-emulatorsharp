using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AwsEmulatorSharp.Emulator.Wire;

public class WireSurface
{
    private const string ActionFormName = "Action";
    private const string InvalidActionCode = "InvalidAction";
    private const string ActionLogMessage = "Request action {Action}";

    private readonly Dictionary<string, IQueryOperation> _operations;
    private readonly QueryResponseWriter _writer;
    private readonly ILogger<WireSurface> _log;

    public WireSurface(IEnumerable<IQueryOperation> operations, QueryResponseWriter writer, ILogger<WireSurface> log)
    {
        _operations = operations.ToDictionary(operation => operation.ActionName);
        _writer = writer;
        _log = log;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();
        var action = form[ActionFormName].ToString();

        _log.LogInformation(ActionLogMessage, action);

        if (!_operations.ContainsKey(action))
        {
            var message = $"The action '{action}' is not implemented by this emulator.";
            await _writer.WriteErrorAsync(context.Response, InvalidActionCode, message);
            return;
        }

        await _operations[action].HandleAsync(context, form);
    }
}
