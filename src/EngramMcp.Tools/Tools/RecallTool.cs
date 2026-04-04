using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed record RecallResponse(
    int SelectedCount,
    int TotalCount,
    IReadOnlyList<RecallMemory> Memories);

public sealed class RecallTool(MemoryService memories) : Tool
{
    private const int MaximumReturnedMemoryCount = 50;

    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load the strongest current memories. Useful at the start of a session. Defaults to returning up to 50 memories unless a maxCount is provided.")]
    public async Task<RecallResponse> ExecuteAsync(
        [Description("Maximum number of memories to return. If omitted or <= 0, defaults to 50.")]
        int? maxCount = null,
        CancellationToken cancellationToken = default)
    {
        var recalledMemories = await memories.RecallAsync(cancellationToken).ConfigureAwait(false);

        var effectiveMaxCount = maxCount.GetValueOrDefault();
        if (effectiveMaxCount <= 0)
            effectiveMaxCount = MaximumReturnedMemoryCount;

        var selected = recalledMemories.Take(effectiveMaxCount).ToArray();
        return new RecallResponse(selected.Length, recalledMemories.Count, selected);
    }
}
