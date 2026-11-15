using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.Extensions.Logging;

namespace AwsEmulatorSharp.Emulator.Tests;

internal class RecordingLogger : ILogger<WireSurface>
{
    private readonly List<(LogLevel Level, string Message)> _entries = new();

    internal IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return new RecordingLogScope();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Add((logLevel, formatter(state, exception)));
    }
}
