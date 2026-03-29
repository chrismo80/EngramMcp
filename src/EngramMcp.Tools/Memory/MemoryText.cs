namespace EngramMcp.Tools.Memory;

internal static class MemoryText
{
    public const int MaxLength = 1000;

    public static string Validate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Memory text must not be null, empty, or whitespace.", nameof(text));

        if (text.Contains('\r') || text.Contains('\n'))
            throw new ArgumentException("Memory text must be a single line without carriage returns or line feeds.", nameof(text));

        if (text.Length > MaxLength)
            throw new ArgumentException($"Memory text must be {MaxLength} characters or fewer.", nameof(text));

        return text;
    }
}
