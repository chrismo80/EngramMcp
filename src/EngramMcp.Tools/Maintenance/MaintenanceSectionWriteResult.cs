namespace EngramMcp.Tools.Maintenance;

public sealed record MaintenanceSectionWriteResult
{
    public required string Section { get; init; }

    public required IReadOnlyList<MaintenanceMemoryEntry> Entries { get; init; }
}
