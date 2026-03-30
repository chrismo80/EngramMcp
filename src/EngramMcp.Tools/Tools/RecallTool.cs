using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed record RecallResponse(int ReturnedCount, int TotalCount, IReadOnlyList<RecallMemory> Memories);

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    private const int MaximumReturnedMemoryCount = 100;

    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load the current memory set. Useful at the start of a session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var memories = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);
        var returnedMemories = memories.Take(MaximumReturnedMemoryCount).ToArray();
        return new RecallResponse(returnedMemories.Length, memories.Count, returnedMemories);
    }
}
