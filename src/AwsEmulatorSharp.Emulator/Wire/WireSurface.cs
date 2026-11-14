using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public class WireSurface
{
    private const string ActionFormName = "Action";
    private const string DescribeRegionsActionName = "DescribeRegions";
    private const string InvalidActionCode = "InvalidAction";

    private readonly DescribeRegionsOperation _describeRegionsOperation;
    private readonly QueryResponseWriter _writer;

    public WireSurface(DescribeRegionsOperation describeRegionsOperation, QueryResponseWriter writer)
    {
        _describeRegionsOperation = describeRegionsOperation;
        _writer = writer;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();
        var action = form[ActionFormName].ToString();

        if (action != DescribeRegionsActionName)
        {
            var message = $"The action '{action}' is not implemented by this emulator.";
            await _writer.WriteErrorAsync(context.Response, InvalidActionCode, message);
            return;
        }

        await _describeRegionsOperation.HandleAsync(context, form);
    }
}
