using EngramMcp.Tools.Memory;

namespace EngramMcp.Tools.Tests;

public sealed class ToolTestMemoryService : IMemoryService
{
    public MemoryChangeResult RememberResult { get; set; }
    public MemoryChangeResult ReinforceResult { get; set; }
    public RetentionTier? RememberedTier { get; private set; }
    public string? RememberedText { get; private set; }
    public IReadOnlyList<string>? ReinforcedMemoryIds { get; private set; }
    public IReadOnlyList<RecallMemory> RecallResult { get; set; } = [];

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
