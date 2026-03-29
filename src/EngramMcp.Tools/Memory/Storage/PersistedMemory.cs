namespace EngramMcp.Tools.Memory.Storage;

public sealed record PersistedMemory
{
    private string _id = null!;
    private string _text = null!;

    public required string Id
    {
        get => _id;
        init => _id = ValidateId(value);
    }

    public required string Text
    {
        get => _text;
        init => _text = MemoryText.Validate(value);
    }

    public required double Retention
    {
        get;
        init
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Retention must be a finite non-negative number.");

            field = value;
        }
    }

    private static string ValidateId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Memory id must not be null, empty, or whitespace.", nameof(id));

        return id.Trim();
    }
}
