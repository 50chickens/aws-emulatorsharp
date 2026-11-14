using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Describe;

internal static class DescribeRequestExtensions
{
    internal static List<string> ReadValues(this IFormCollection form, string parameterPrefix)
    {
        return form
            .Where(parameter => parameter.Key.StartsWith(parameterPrefix, StringComparison.Ordinal))
            .Select(parameter => parameter.Value.ToString())
            .ToList();
    }

    internal static List<T> SelectRequested<T>(this List<T> items, List<string> requestedIdentifiers, Func<T, string> identifier)
    {
        if (requestedIdentifiers.Count == 0)
        {
            return items;
        }

        return items.Where(item => requestedIdentifiers.Contains(identifier(item))).ToList();
    }
}
