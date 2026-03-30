using EngramMcp.Tools.Memory.Retention;

namespace EngramMcp.Tools.Memory;

public interface IMemoryService
{
    Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default);
    Task<MemoryChangeResult> RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default);
    Task<MemoryChangeResult> ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default);
}
