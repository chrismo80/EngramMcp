using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;

namespace EngramMcp.Tools.Tests.Tools;

internal sealed class ToolTestMemoryService : IMemoryService
{
    public RetentionTier? RememberedTier { get; private set; }
    public string? RememberedText { get; private set; }
    public IReadOnlyList<string>? ReinforcedMemoryIds { get; private set; }
    public IReadOnlyList<RecallMemory> RecallResult { get; init; } = [];

    public Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecallResult);
    }

    public Task RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default)
    {
        RememberedTier = retentionTier;
        RememberedText = text;
        return Task.CompletedTask;
    }

    public Task ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default)
    {
        ReinforcedMemoryIds = memoryIds;
        return Task.CompletedTask;
    }
}
