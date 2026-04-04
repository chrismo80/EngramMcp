using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed class ForgetTool(MemoryService memories) : Tool
{
    [McpServerTool(Name = "forget", Title = "Forget Memories")]
    [Description("Delete memories by id. Use this when a previously stored memory is wrong or no longer relevant. Prefer targeted deletions; do not mass-delete without a clear reason.")]
    public async Task<string?> ExecuteAsync(
        [Description("The memory ids to delete.")]
        IReadOnlyList<string> memoryIds,
        CancellationToken cancellationToken = default)
    {
        var result = await memories.ForgetAsync(memoryIds, cancellationToken).ConfigureAwait(false);
        return result.Rejection;
    }
}

