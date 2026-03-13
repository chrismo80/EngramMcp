namespace EngramMcp.Core;

public sealed record MemoryEntry
{
    public MemoryEntry(DateTime timestamp, string text, IEnumerable<string>? tags = null, MemoryImportance? importance = null)
    {
        Timestamp = timestamp;
        Text = text;
        Tags = NormalizeTags(tags);
        Importance = importance ?? MemoryImportance.Normal;
    }

    public DateTime Timestamp { get; init; }

    public string Text { get; init; }

    public IReadOnlyList<string> Tags { get; init; }

    public MemoryImportance Importance { get; init; }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
            return [];

        var normalizedTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            var normalizedTag = tag.Trim().ToLowerInvariant();

            if (seenTags.Add(normalizedTag))
                normalizedTags.Add(normalizedTag);
        }

        return normalizedTags.Count == 0 ? [] : normalizedTags.AsReadOnly();
    }
}
