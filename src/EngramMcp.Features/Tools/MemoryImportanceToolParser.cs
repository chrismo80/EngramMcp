using EngramMcp.Core;

namespace EngramMcp.Features.Tools;

internal static class MemoryImportanceToolParser
{
    private const string AllowedValues = "low, normal, high";

    public static MemoryImportance? ParseOrDefault(string? importance)
    {
        if (importance is null)
        {
            return null;
        }

        if (MemoryImportanceSerializer.TryParse(importance, out var parsedImportance))
        {
            return parsedImportance;
        }

        throw new ArgumentException(
            $"Invalid importance '{importance}'. Allowed values: {AllowedValues}.",
            nameof(importance));
    }
}
