namespace EngramMcp.Core;

internal static class MemoryRetention
{
    public static int GetNextEntryIndexToEvict(IReadOnlyList<MemoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
            throw new ArgumentException("At least one memory entry is required to select an eviction victim.", nameof(entries));

        var victimIndex = 0;

        for (var index = 1; index < entries.Count; index++)
        {
            if (ShouldEvictBefore(entries[index], entries[victimIndex]))
                victimIndex = index;
        }

        return victimIndex;
    }

    private static bool ShouldEvictBefore(MemoryEntry candidate, MemoryEntry currentVictim)
    {
        if (candidate.Importance != currentVictim.Importance)
            return candidate.Importance < currentVictim.Importance;

        return candidate.Timestamp < currentVictim.Timestamp;
    }
}
