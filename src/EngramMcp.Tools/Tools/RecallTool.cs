using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed record RecallResponse(IReadOnlyList<RecallMemory> Memories);

public sealed class RecallTool(MemoryService memories) : Tool
{
    private const int MaximumReturnedMemoryCount = 50;

    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load up to the 100 strongest current memories. Useful at the start of a session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var recalledMemories = await memories.RecallAsync(cancellationToken).ConfigureAwait(false);

        return new RecallResponse(recalledMemories.Take(MaximumReturnedMemoryCount).ToArray());
    }
}