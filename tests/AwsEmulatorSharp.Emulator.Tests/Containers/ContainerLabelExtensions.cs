namespace AwsEmulatorSharp.Emulator.Tests;

internal static class ContainerLabelExtensions
{
    internal static bool HasLabel(this IReadOnlyDictionary<string, string> labels, string labelKey, string labelValue)
    {
        var hasLabelKey = labels.ContainsKey(labelKey);
        if (!hasLabelKey)
        {
            return false;
        }

        var currentValue = labels[labelKey];
        var isMatch = string.Equals(currentValue, labelValue, StringComparison.Ordinal);
        return isMatch;
    }
}
