namespace EngramMcp.Core;

public sealed record MaintenanceSectionWriteResult
{
    public required string Section { get; init; }

    public required IReadOnlyList<MaintenanceMemoryEntry> Entries { get; init; }
}
