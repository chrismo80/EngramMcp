namespace EngramMcp.Core;

public sealed record MemoryEntry
{
    private const int MaxTextLength = 280;

    private string _text = null!;

    public MemoryEntry(DateTime timestamp, string text, IEnumerable<string>? tags = null, MemoryImportance? importance = null)
    {
        Timestamp = timestamp;
        Text = text;
        Tags = NormalizeTags(tags);
        Importance = importance ?? MemoryImportance.Normal;
    }

    public DateTime Timestamp { get; init; }

    public string Text
    {
        get => _text;
        init => _text = ValidateText(value);
    }

    public IReadOnlyList<string> Tags { get; init; }

    public MemoryImportance Importance { get; init; }

    private static string ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Memory text must not be null, empty, or whitespace.", nameof(text));

        if (text.Contains('\r') || text.Contains('\n'))
            throw new ArgumentException("Memory text must be a single line without carriage returns or line feeds.", nameof(text));

        if (text.Length > MaxTextLength)
            throw new ArgumentException($"Memory text must be {MaxTextLength} characters or fewer.", nameof(text));

        return text;
    }

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
