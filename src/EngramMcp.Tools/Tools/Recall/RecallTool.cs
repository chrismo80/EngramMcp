using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Recall;

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load the current memory set. Useful at the start of a session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var memories = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);
        return new RecallResponse(memories);
    }
}
