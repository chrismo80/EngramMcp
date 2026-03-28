using EngramMcp.Tools.Maintenance;

namespace EngramMcp.Tools.Memory;

public interface IMemoryService
{
    Task StoreAsync(
        string section,
        string text,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default);

    Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default);

    Task<MaintenanceSectionReadResult> ReadForMaintenanceAsync(string section, CancellationToken cancellationToken = default);

    Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default);

    Task<MaintenanceSectionWriteResult> WriteForMaintenanceAsync(
        string section,
        string consolidationToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries,
        CancellationToken cancellationToken = default);
}
