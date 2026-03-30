namespace EngramMcp.Tools.Memory;

public readonly record struct MemoryChangeResult
{
    private MemoryChangeResult(string rejection)
    {
        Rejection = rejection;
    }

    public string? Rejection { get; }

    public bool Succeeded => Rejection is null;

    public static MemoryChangeResult Success() => default;

    public static MemoryChangeResult Reject(string rejection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rejection);
        return new MemoryChangeResult(rejection);
    }
}
