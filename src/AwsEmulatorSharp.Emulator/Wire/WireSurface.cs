using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public class WireSurface
{
    private const string ActionFormName = "Action";
    private const string InvalidActionCode = "InvalidAction";

    private readonly Dictionary<string, IQueryOperation> _operations;
    private readonly QueryResponseWriter _writer;

    public WireSurface(IEnumerable<IQueryOperation> operations, QueryResponseWriter writer)
    {
        _operations = operations.ToDictionary(operation => operation.ActionName);
        _writer = writer;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();
        var action = form[ActionFormName].ToString();

        if (!_operations.ContainsKey(action))
        {
            var message = $"The action '{action}' is not implemented by this emulator.";
            await _writer.WriteErrorAsync(context.Response, InvalidActionCode, message);
            return;
        }

        await _operations[action].HandleAsync(context, form);
    }
}
