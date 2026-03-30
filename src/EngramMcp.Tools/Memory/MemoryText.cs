namespace EngramMcp.Tools.Memory;

internal static class MemoryText
{
    public const int MaxLength = 1000;

    public static string? GetValidationError(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Memory text must not be null, empty, or whitespace.";

        if (text.Contains('\r') || text.Contains('\n'))
            return "Memory text must be a single line without carriage returns or line feeds.";

        return text.Length > MaxLength
            ? $"Memory text must be {MaxLength} characters or fewer."
            : null;
    }

    public static string Validate(string? text)
    {
        var validationError = GetValidationError(text);

        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(text));

        return text!;
    }
}
