using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed record RecallResponse(IReadOnlyList<RecallMemory> Memories);

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    private const int MaximumReturnedMemoryCount = 100;

    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load up to the 100 strongest current memories. Useful at the start of a session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var memories = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);

        return new RecallResponse(memories.Take(MaximumReturnedMemoryCount).ToArray());
    }
}
