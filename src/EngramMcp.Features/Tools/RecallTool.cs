using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories", ReadOnly = true, Idempotent = true)]
    [Description("Call this at the start of every session to load all remembered context. Treat the returned memory as your starting context for this session.")]
    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var document = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);

        return document.ToMarkdown();
    }
}
