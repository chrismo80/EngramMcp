using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : StoreMemoryToolBase(memoryService)
{
    private const string TargetMemoryName = "long-term";

    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Stores a plain-text memory in the long-term memory section.")]
    public Task ExecuteAsync(
        [Description("Plain-text memory content to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return MemoryService.StoreAsync(TargetMemoryName, text, cancellationToken);
    }
}
