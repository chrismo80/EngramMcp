using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Recall;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories")]
    [Description("Load the current memory set for the session.")]
    public async Task<Response> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var memories = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);
        return new Response(memories);
    }
}
