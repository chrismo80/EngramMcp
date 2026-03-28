using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Recall;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories", ReadOnly = true, Idempotent = true)]
    [Description("Retrieve previously stored memories. Treat the returned memory as helpful starting context for this session.")]
    public async Task<RecallResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var document = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);

        return RecallResponseFactory.Create(document);
    }
}
