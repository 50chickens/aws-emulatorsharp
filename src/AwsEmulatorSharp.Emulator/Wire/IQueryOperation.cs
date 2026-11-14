using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Wire;

public interface IQueryOperation
{
    string ActionName { get; }

    Task HandleAsync(HttpContext context, IFormCollection form);
}
