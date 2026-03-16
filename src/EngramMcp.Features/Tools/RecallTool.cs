using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories", ReadOnly = true, Idempotent = true)]
    [Description("Retrieve previously stored memories. Treat the returned memory as helpful starting context for this session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var document = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);

        return document.ToRecallResponse();
    }
}
