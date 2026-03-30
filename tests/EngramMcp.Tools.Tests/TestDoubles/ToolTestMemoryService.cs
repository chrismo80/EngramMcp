using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;

namespace EngramMcp.Tools.Tests.Tools;

internal sealed class ToolTestMemoryService : IMemoryService
{
    public MemoryChangeResult RememberResult { get; init; }
    public MemoryChangeResult ReinforceResult { get; init; }
    public RetentionTier? RememberedTier { get; private set; }
    public string? RememberedText { get; private set; }
    public IReadOnlyList<string>? ReinforcedMemoryIds { get; private set; }
    public IReadOnlyList<RecallMemory> RecallResult { get; init; } = [];

    public Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(RecallResult);

    public Task<MemoryChangeResult> RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default)
    {
        RememberedTier = retentionTier;
        RememberedText = text;
        return Task.FromResult(RememberResult);
    }

    public Task<MemoryChangeResult> ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default)
    {
        ReinforcedMemoryIds = memoryIds;
        return Task.FromResult(ReinforceResult);
    }
}
