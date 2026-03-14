namespace EngramMcp.Core;

internal static class MemoryRetention
{
    public static MemoryEntry GetEntryToEvict(this IReadOnlyList<MemoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
            throw new ArgumentException("At least one memory entry is required to select an eviction victim.", nameof(entries));

        return entries
            .OrderBy(e => e.Importance)
            .ThenBy(e => e.Timestamp)
            .First();
    }
}