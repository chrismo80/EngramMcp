namespace EngramMcp.Tools.Maintenance;

public sealed record MaintenanceSectionReadResult
{
    public required string Section { get; init; }

    public required IReadOnlyList<MaintenanceMemoryEntry> Entries { get; init; }

    public required string ConsolidationToken { get; init; }
}
