using EngramMcp.Tools.Maintenance;
using EngramMcp.Tools.Memory;

namespace EngramMcp.Tools.Tests.TestDoubles;

internal sealed class ToolTestMemoryService : IMemoryService
{
    public string? StoredSection { get; private set; }
    public string? StoredText { get; private set; }
    public MemoryImportance? StoredImportance { get; private set; }
    public MemoryContainer ReadResult { get; init; } = new();
    public MemoryContainer RecallResult { get; init; } = new();
    public MaintenanceSectionReadResult MaintenanceReadResult { get; init; } = new()
    {
        Section = "project-x",
        Entries = [],
        ConsolidationToken = "token-1"
    };
    public MaintenanceSectionWriteResult MaintenanceWriteResult { get; init; } = new()
    {
        Section = "project-x",
        Entries = []
    };

    public Task StoreAsync(string section, string text, MemoryImportance? importance = null, CancellationToken cancellationToken = default)
    {
        StoredSection = section;
        StoredText = text;
        StoredImportance = importance;
        return Task.CompletedTask;
    }

    public Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ReadResult);
    }

    public Task<MaintenanceSectionReadResult> ReadForMaintenanceAsync(string section, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MaintenanceReadResult);
    }

    public Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecallResult);
    }

    public Task<MaintenanceSectionWriteResult> WriteForMaintenanceAsync(
        string section,
        string consolidationToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MaintenanceWriteResult);
    }
}
