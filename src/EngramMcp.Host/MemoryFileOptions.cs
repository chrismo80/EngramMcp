using EngramMcp.Infrastructure.Memory;

namespace EngramMcp.Host;

public sealed class MemoryFileOptions
{
    public required string FilePath { get; init; }

    public MemorySize Size { get; init; } = MemorySize.Normal;
}
