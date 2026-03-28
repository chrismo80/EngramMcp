namespace EngramMcp.Tools.Memory;

public sealed record MemoryEntry
{
    private const int MaxTextLength = 500;

    private string _text = null!;

    public MemoryEntry(DateTime timestamp, string text, MemoryImportance? importance = null)
    {
        Timestamp = timestamp;
        Text = text;
        Importance = importance ?? MemoryImportance.Normal;
    }

    public DateTime Timestamp { get; }

    public string Text
    {
        get => _text;
        private init => _text = ValidateText(value);
    }

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
}
