using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories", ReadOnly = true, Idempotent = true)]
    [Description("Loads and returns the raw content of all configured memory sections.")]
    public Task<MemoryDocument> ExecuteAsync(CancellationToken cancellationToken)
    {
        return memoryService.RecallAsync(cancellationToken);
    }
}
